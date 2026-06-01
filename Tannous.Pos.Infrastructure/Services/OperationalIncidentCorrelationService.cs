using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Infrastructure.Services;

public class OperationalIncidentCorrelationService : IOperationalIncidentCorrelationService
{
    private static readonly string[] UnresolvedStatuses =
    {
        nameof(ReconciliationResolutionStatus.Unresolved),
        nameof(ReconciliationResolutionStatus.Acknowledged),
        nameof(ReconciliationResolutionStatus.Investigating)
    };

    private readonly PosDbContext _db;
    private readonly IOperationalAuditPersistenceTelemetry _auditTelemetry;
    private readonly IOperationalResiliencePressureState _pressureState;
    private readonly IOperationalDiagnosticsCache _cache;
    private readonly IOperationalDiagnosticsCacheTelemetry _cacheTelemetry;
    private readonly ILogger<OperationalIncidentCorrelationService> _logger;

    public OperationalIncidentCorrelationService(
        PosDbContext db,
        IOperationalAuditPersistenceTelemetry auditTelemetry,
        IOperationalResiliencePressureState pressureState,
        IOperationalDiagnosticsCache cache,
        IOperationalDiagnosticsCacheTelemetry cacheTelemetry,
        ILogger<OperationalIncidentCorrelationService> logger)
    {
        _db = db;
        _auditTelemetry = auditTelemetry;
        _pressureState = pressureState;
        _cache = cache;
        _cacheTelemetry = cacheTelemetry;
        _logger = logger;
    }

    public async Task<OperationalIncidentSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var groups = await GetIncidentGroupsCachedAsync(cancellationToken).ConfigureAwait(false);
        LogCorrelationRisk(groups, "Summary");

        var bySeverity = groups
            .GroupBy(g => g.Severity)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var overall = groups.Count == 0
            ? OperationalIncidentSeverity.Low
            : OperationalIncidentRiskClassifier.GetMaxSeverity(groups.Select(g => g.Severity));

        _logger.LogInformation(
            "Operational incident observability: incident summary generated. TotalGroups={Total}, HighRisk={HighRisk}, OverallRisk={OverallRisk}",
            groups.Count,
            groups.Count(g => OperationalIncidentRiskClassifier.IsHighRisk(g.Severity)),
            overall);

        return new OperationalIncidentSummaryDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            TotalIncidentGroups = groups.Count,
            HighRiskIncidentCount = groups.Count(g => g.Severity == OperationalIncidentSeverity.High),
            CriticalIncidentCount = groups.Count(g => g.Severity == OperationalIncidentSeverity.Critical),
            ReplayIncidentCount = groups.Count(g => g.PrimaryIncidentType == OperationalIncidentTypes.ReplayIncident),
            ReconciliationIncidentCount = groups.Count(g => g.PrimaryIncidentType == OperationalIncidentTypes.ReconciliationIncident),
            CascadingDegradationCount = groups.Count(g => g.PrimaryIncidentType == OperationalIncidentTypes.CascadingDegradationIncident),
            OverallCorrelatedRisk = overall,
            IncidentsBySeverity = bySeverity,
            CorrelationGuidance = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["causality"] = OperationalIncidentGovernance.GetCausalityAssumption(OperationalIncidentTypes.ReplayIncident),
                ["grouping"] = OperationalIncidentGovernance.GetSubsystemGroupingRule(),
                ["nonGoals"] = "no PagerDuty; no OpenTelemetry; no automatic remediation; dynamic correlation only"
            }
        };
    }

    public async Task<OperationalIncidentPageDto> GetHighRiskAsync(CancellationToken cancellationToken = default)
    {
        var groups = await GetIncidentGroupsCachedAsync(cancellationToken).ConfigureAwait(false);
        var high = groups
            .Where(g => OperationalIncidentRiskClassifier.IsHighRisk(g.Severity))
            .Take(OperationalIncidentCorrelationConstants.MaxIncidentsReturned)
            .ToList();

        LogCorrelationRisk(high, "HighRisk");
        return new OperationalIncidentPageDto { Items = high, Total = high.Count };
    }

    public Task<OperationalIncidentPageDto> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        GetFilteredAsync(g => g.OrderId == orderId, cancellationToken);

    public Task<OperationalIncidentPageDto> GetByDeviceIdAsync(string deviceId, CancellationToken cancellationToken = default) =>
        GetFilteredAsync(g => string.Equals(g.DeviceId, deviceId, StringComparison.Ordinal), cancellationToken);

    public Task<OperationalIncidentPageDto> GetByOperationIdAsync(string operationId, CancellationToken cancellationToken = default) =>
        GetFilteredAsync(g => string.Equals(g.OperationId, operationId, StringComparison.Ordinal), cancellationToken);

    public async Task<OperationalCascadingDegradationDto> GetCascadingDegradationAsync(
        CancellationToken cancellationToken = default)
    {
        var groups = await GetIncidentGroupsCachedAsync(cancellationToken).ConfigureAwait(false);
        var cascading = groups
            .Where(g => g.PrimaryIncidentType == OperationalIncidentTypes.CascadingDegradationIncident
                || g.CorrelatedSubsystems.Count >= OperationalIncidentCorrelationConstants.CascadingSubsystemMinimum)
            .Select(g => new CascadingDegradationPatternDto
            {
                CorrelationKey = BuildCorrelationKeyDisplay(g),
                Severity = g.Severity,
                Subsystems = g.CorrelatedSubsystems,
                CausalityHint = g.CausalityHint,
                SignalCount = g.SignalCount
            })
            .Take(OperationalIncidentCorrelationConstants.MaxIncidentsReturned)
            .ToList();

        _logger.LogInformation(
            "Operational causality visibility: cascading degradation patterns aggregated. PatternCount={Count}",
            cascading.Count);

        return new OperationalCascadingDegradationDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            CascadingPatternCount = cascading.Count,
            Patterns = cascading
        };
    }

    public ForensicIncidentCorrelationDto BuildForensicCorrelation(
        IReadOnlyList<ConflictSnapshotItemDto> conflicts,
        IReadOnlyList<AuditTimelineSnapshotItemDto> audits,
        ForensicTruncationFlags truncationFlags)
    {
        var signals = BuildSignalsFromSnapshot(conflicts, audits, truncationFlags);
        var groups = GroupSignals(signals);
        var primary = groups.FirstOrDefault();
        var risk = primary?.CorrelatedRisk ?? OperationalIncidentRiskClassifier.ClassifyCorrelatedRisk(signals);
        var subsystems = primary?.CorrelatedSubsystems
            ?? signals.Select(s => s.Subsystem).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var summary = groups.Count == 0
            ? "No correlated incident groups in export scope."
            : $"Correlated {groups.Count} incident group(s); primary={primary?.PrimaryIncidentType ?? "none"}; risk={risk}.";

        if (signals.Any(s => s.IncidentType == OperationalIncidentTypes.ReplayIncident)
            && signals.Any(s => s.IncidentType == OperationalIncidentTypes.ReconciliationIncident))
        {
            _logger.LogWarning(
                "Operational causality visibility: replay mismatch correlated with reconciliation signals in forensic export.");
        }

        _logger.LogInformation(
            "Operational correlation risk: forensic incident enrichment applied. Risk={Risk}, SubsystemCount={SubsystemCount}",
            risk,
            subsystems.Count);

        return new ForensicIncidentCorrelationDto
        {
            CorrelatedIncidentRisk = risk,
            CorrelatedSubsystems = subsystems,
            IncidentCorrelationSummary = summary
        };
    }

    private async Task<OperationalIncidentPageDto> GetFilteredAsync(
        Func<CorrelatedIncidentItemDto, bool> predicate,
        CancellationToken cancellationToken)
    {
        var groups = await GetIncidentGroupsCachedAsync(cancellationToken).ConfigureAwait(false);
        var filtered = groups.Where(predicate).ToList();
        LogCorrelationRisk(filtered, "Filtered");
        return new OperationalIncidentPageDto
        {
            Items = filtered.Take(OperationalIncidentCorrelationConstants.MaxIncidentsReturned).ToList(),
            Total = filtered.Count
        };
    }

    private async Task<IReadOnlyList<CorrelatedIncidentItemDto>> GetIncidentGroupsCachedAsync(
        CancellationToken cancellationToken)
    {
        var category = OperationalDiagnosticsCacheCategories.IncidentGroups;
        var pressureSignals = OperationalCacheAdaptivePressureSignalBuilder.ForIncident(
            _pressureState,
            _cache,
            IsReplayStormRiskForAdaptiveTtl);
        var effectiveTtl = OperationalCacheAdaptiveTtlHelper.ResolveEffectiveTtl(
            category,
            pressureSignals,
            _cache,
            _cacheTelemetry,
            _logger,
            out _);

        var envelope = await _cache.GetOrCreateAsync(
            OperationalDiagnosticsCacheConstants.IncidentGroupsCacheKey,
            category,
            effectiveTtl,
            BuildIncidentGroupsAsync,
            bypass: false,
            cancellationToken).ConfigureAwait(false);

        return envelope.Value;
    }

    private async Task<List<CorrelatedIncidentItemDto>> BuildIncidentGroupsAsync(CancellationToken cancellationToken)
    {
        var signals = await LoadSignalsAsync(cancellationToken).ConfigureAwait(false);
        return GroupSignals(signals);
    }

    private async Task<List<OperationalIncidentSignal>> LoadSignalsAsync(CancellationToken cancellationToken)
    {
        var signals = new List<OperationalIncidentSignal>();

        var conflicts = await _db.SyncConflictRecords.AsNoTracking()
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(OperationalIncidentCorrelationConstants.MaxSignalsPerCorrelationQuery)
            .ToListAsync(cancellationToken);

        foreach (var c in conflicts)
        {
            var key = BuildCorrelationKey(c.OperationId, c.DeviceId, null, c.EntityId);
            var type = ClassifyConflictIncidentType(c.ConflictType);
            signals.Add(new OperationalIncidentSignal
            {
                IncidentType = type,
                Subsystem = MapSubsystem(type),
                DeviceId = c.DeviceId,
                OperationId = c.OperationId,
                EntityId = c.EntityId,
                OrderId = c.EntityType == "Order" ? c.EntityId : null,
                ConflictType = c.ConflictType,
                ResolutionStatus = c.ResolutionStatus,
                TimestampUtc = c.CreatedAtUtc,
                CorrelationKey = key
            });

            if (UnresolvedStatuses.Contains(c.ResolutionStatus))
            {
                signals.Add(new OperationalIncidentSignal
                {
                    IncidentType = OperationalIncidentTypes.ReconciliationIncident,
                    Subsystem = "Reconciliation",
                    DeviceId = c.DeviceId,
                    OperationId = c.OperationId,
                    EntityId = c.EntityId,
                    OrderId = c.EntityType == "Order" ? c.EntityId : null,
                    ConflictType = c.ConflictType,
                    ResolutionStatus = c.ResolutionStatus,
                    TimestampUtc = c.CreatedAtUtc,
                    CorrelationKey = key
                });
            }
        }

        var auditActions = new[]
        {
            OperationalAuditActions.ReplayMismatch,
            OperationalAuditActions.ConcurrencyConflict,
            OperationalAuditActions.NegativeStockDetected,
            OperationalAuditActions.LifecycleStateConflict,
            OperationalAuditActions.PartialBatchReconciliation,
            OperationalAuditActions.SettlementOverpayment,
            OperationalAuditActions.SettlementUnderpaymentRejected,
            OperationalAuditActions.MixedBatchOutcomes
        };

        var audits = await _db.OperationalAuditRecords.AsNoTracking()
            .Where(r => auditActions.Contains(r.Action))
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(OperationalIncidentCorrelationConstants.MaxSignalsPerCorrelationQuery)
            .ToListAsync(cancellationToken);

        foreach (var a in audits)
        {
            var key = BuildCorrelationKey(a.OperationId, a.DeviceId, a.OrderId, a.EntityId);
            var type = ClassifyAuditIncidentType(a.Action);
            signals.Add(new OperationalIncidentSignal
            {
                IncidentType = type,
                Subsystem = MapSubsystem(type),
                DeviceId = a.DeviceId,
                OperationId = a.OperationId,
                OrderId = a.OrderId,
                EntityId = a.EntityId,
                AuditAction = a.Action,
                TimestampUtc = a.CreatedAtUtc,
                CorrelationKey = key
            });
        }

        AppendResilienceSignals(signals);
        AppendCascadingSignals(signals);
        return signals;
    }

    private void AppendResilienceSignals(List<OperationalIncidentSignal> signals)
    {
        if (_auditTelemetry.GetRecentFailureCount() > 0)
        {
            signals.Add(new OperationalIncidentSignal
            {
                IncidentType = OperationalIncidentTypes.ResiliencePressureIncident,
                Subsystem = "AuditPersistence",
                TimestampUtc = DateTime.UtcNow,
                CorrelationKey = "resilience:audit-persistence"
            });
        }

        if (_pressureState.QueryDateRangeClamped || _pressureState.QueryPageSizeClamped)
        {
            signals.Add(new OperationalIncidentSignal
            {
                IncidentType = OperationalIncidentTypes.ResiliencePressureIncident,
                Subsystem = "DiagnosticsQuery",
                TimestampUtc = DateTime.UtcNow,
                CorrelationKey = "resilience:query-pressure"
            });
        }

        if (_pressureState.ForensicExportTruncated)
        {
            signals.Add(new OperationalIncidentSignal
            {
                IncidentType = OperationalIncidentTypes.ForensicSurvivabilityIncident,
                Subsystem = "ForensicExport",
                TimestampUtc = DateTime.UtcNow,
                CorrelationKey = "resilience:forensic-export"
            });
        }
    }

    private static void AppendCascadingSignals(List<OperationalIncidentSignal> signals)
    {
        foreach (var group in signals.GroupBy(s => s.CorrelationKey))
        {
            var types = group.Select(s => s.IncidentType).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (types.Count < OperationalIncidentCorrelationConstants.CascadingSubsystemMinimum)
                continue;

            var first = group.First();
            signals.Add(new OperationalIncidentSignal
            {
                IncidentType = OperationalIncidentTypes.CascadingDegradationIncident,
                Subsystem = "Cascading",
                DeviceId = first.DeviceId,
                OperationId = first.OperationId,
                OrderId = first.OrderId,
                EntityId = first.EntityId,
                TimestampUtc = group.Max(s => s.TimestampUtc),
                CorrelationKey = group.Key
            });
        }
    }

    private static List<OperationalIncidentSignal> BuildSignalsFromSnapshot(
        IReadOnlyList<ConflictSnapshotItemDto> conflicts,
        IReadOnlyList<AuditTimelineSnapshotItemDto> audits,
        ForensicTruncationFlags truncationFlags)
    {
        var signals = new List<OperationalIncidentSignal>();

        foreach (var c in conflicts)
        {
            var key = BuildCorrelationKey(c.OperationId, c.DeviceId, null, c.EntityId);
            var type = ClassifyConflictIncidentType(c.ConflictType);
            signals.Add(new OperationalIncidentSignal
            {
                IncidentType = type,
                Subsystem = MapSubsystem(type),
                DeviceId = c.DeviceId,
                OperationId = c.OperationId,
                EntityId = c.EntityId,
                ConflictType = c.ConflictType,
                ResolutionStatus = c.ResolutionStatus,
                TimestampUtc = c.CreatedAtUtc,
                CorrelationKey = key
            });
        }

        foreach (var a in audits)
        {
            var key = BuildCorrelationKey(a.OperationId, a.DeviceId, a.OrderId, a.EntityId);
            var type = ClassifyAuditIncidentType(a.Action);
            signals.Add(new OperationalIncidentSignal
            {
                IncidentType = type,
                Subsystem = MapSubsystem(type),
                DeviceId = a.DeviceId,
                OperationId = a.OperationId,
                OrderId = a.OrderId,
                EntityId = a.EntityId,
                AuditAction = a.Action,
                TimestampUtc = a.TimestampUtc,
                CorrelationKey = key
            });
        }

        if (truncationFlags.AnyTruncated)
        {
            signals.Add(new OperationalIncidentSignal
            {
                IncidentType = OperationalIncidentTypes.ForensicSurvivabilityIncident,
                Subsystem = "ForensicExport",
                TimestampUtc = DateTime.UtcNow,
                CorrelationKey = "forensic:export-truncation"
            });
        }

        AppendCascadingSignals(signals);
        return signals;
    }

    private static List<CorrelatedIncidentItemDto> GroupSignals(List<OperationalIncidentSignal> signals)
    {
        return signals
            .GroupBy(s => s.CorrelationKey)
            .Select(g =>
            {
                var list = g.ToList();
                var severity = OperationalIncidentRiskClassifier.ClassifySeverity(list);
                var primaryType = list
                    .GroupBy(s => s.IncidentType)
                    .OrderByDescending(x => x.Count())
                    .First()
                    .Key;
                var subsystems = list.Select(s => s.Subsystem).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                var first = list.MinBy(s => s.TimestampUtc)!;
                var last = list.MaxBy(s => s.TimestampUtc)!;

                return new CorrelatedIncidentItemDto
                {
                    IncidentGroupId = CreateDeterministicGroupId(g.Key),
                    PrimaryIncidentType = primaryType,
                    Severity = severity,
                    CorrelatedRisk = severity,
                    CorrelatedSubsystems = subsystems,
                    CausalityHint = OperationalIncidentGovernance.GetCausalityAssumption(primaryType),
                    OrderId = first.OrderId,
                    DeviceId = first.DeviceId,
                    OperationId = first.OperationId,
                    EntityId = first.EntityId,
                    SignalCount = list.Count,
                    FirstSeenUtc = first.TimestampUtc,
                    LastSeenUtc = last.TimestampUtc
                };
            })
            .OrderByDescending(g => g.Severity, StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(g => g.LastSeenUtc)
            .ToList();
    }

    private void LogCorrelationRisk(IReadOnlyList<CorrelatedIncidentItemDto> groups, string scope)
    {
        if (groups.Any(g => g.Severity == OperationalIncidentSeverity.Critical))
        {
            _logger.LogWarning(
                "Operational correlation risk: critical incidents detected. Scope={Scope}, Count={Count}",
                scope,
                groups.Count(g => g.Severity == OperationalIncidentSeverity.Critical));
        }
    }

    private static string ClassifyConflictIncidentType(string conflictType)
    {
        if (conflictType.Contains("ReplayMismatch", StringComparison.OrdinalIgnoreCase))
            return OperationalIncidentTypes.ReplayIncident;
        if (conflictType.Contains("InventoryDrift", StringComparison.OrdinalIgnoreCase)
            || conflictType.Contains("NegativeStock", StringComparison.OrdinalIgnoreCase))
            return OperationalIncidentTypes.InventoryDriftIncident;
        if (conflictType.Contains("Concurrency", StringComparison.OrdinalIgnoreCase))
            return OperationalIncidentTypes.SettlementInconsistencyIncident;
        return OperationalIncidentTypes.ReconciliationIncident;
    }

    private static string ClassifyAuditIncidentType(string action) =>
        action switch
        {
            OperationalAuditActions.ReplayMismatch => OperationalIncidentTypes.ReplayIncident,
            OperationalAuditActions.NegativeStockDetected => OperationalIncidentTypes.InventoryDriftIncident,
            OperationalAuditActions.SettlementOverpayment or OperationalAuditActions.SettlementUnderpaymentRejected
                => OperationalIncidentTypes.SettlementInconsistencyIncident,
            OperationalAuditActions.ConcurrencyConflict => OperationalIncidentTypes.SettlementInconsistencyIncident,
            _ => OperationalIncidentTypes.ReconciliationIncident
        };

    private static string MapSubsystem(string incidentType) =>
        incidentType switch
        {
            OperationalIncidentTypes.ReplayIncident => "SyncReplay",
            OperationalIncidentTypes.ReconciliationIncident => "Reconciliation",
            OperationalIncidentTypes.SettlementInconsistencyIncident => "Settlement",
            OperationalIncidentTypes.InventoryDriftIncident => "Inventory",
            OperationalIncidentTypes.ResiliencePressureIncident => "Resilience",
            OperationalIncidentTypes.ForensicSurvivabilityIncident => "ForensicExport",
            OperationalIncidentTypes.CascadingDegradationIncident => "Cascading",
            _ => "Operational"
        };

    private static string BuildCorrelationKey(string? operationId, string? deviceId, Guid? orderId, Guid? entityId)
    {
        if (!string.IsNullOrWhiteSpace(operationId))
            return $"op:{operationId}";
        if (!string.IsNullOrWhiteSpace(deviceId))
            return $"dev:{deviceId}";
        if (orderId.HasValue)
            return $"ord:{orderId}";
        if (entityId.HasValue)
            return $"ent:{entityId}";
        return "global:operational";
    }

    private static string BuildCorrelationKeyDisplay(CorrelatedIncidentItemDto g)
    {
        if (!string.IsNullOrWhiteSpace(g.OperationId))
            return $"operation:{g.OperationId}";
        if (!string.IsNullOrWhiteSpace(g.DeviceId))
            return $"device:{g.DeviceId}";
        if (g.OrderId.HasValue)
            return $"order:{g.OrderId}";
        return "global";
    }

    private static Guid CreateDeterministicGroupId(string correlationKey)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(correlationKey));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        return new Guid(bytes);
    }

    private static bool IsReplayStormRiskForAdaptiveTtl(OperationalResilienceMetricsSnapshot metrics) =>
        metrics.MaxReplayReceiptsOnSingleDevice >= OperationalResilienceConstants.ReplayStormDeviceReceiptThreshold
        || metrics.ReplayReceiptCount >= OperationalResilienceConstants.ReplayStormReceiptCountThreshold;
}
