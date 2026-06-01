using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Infrastructure.Services;

public class OperationalResilienceDiagnosticsService : IOperationalResilienceDiagnosticsService
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
    private readonly ILogger<OperationalResilienceDiagnosticsService> _logger;

    public OperationalResilienceDiagnosticsService(
        PosDbContext db,
        IOperationalAuditPersistenceTelemetry auditTelemetry,
        IOperationalResiliencePressureState pressureState,
        IOperationalDiagnosticsCache cache,
        IOperationalDiagnosticsCacheTelemetry cacheTelemetry,
        ILogger<OperationalResilienceDiagnosticsService> logger)
    {
        _db = db;
        _auditTelemetry = auditTelemetry;
        _pressureState = pressureState;
        _cache = cache;
        _cacheTelemetry = cacheTelemetry;
        _logger = logger;
    }

    public void NoteQueryPressure(bool dateRangeClamped, bool pageSizeClamped)
    {
        _pressureState.NoteQueryPressure(dateRangeClamped, pageSizeClamped);

        if (dateRangeClamped || pageSizeClamped)
        {
            _logger.LogWarning(
                "Operational backpressure visibility: query pressure noted. DateRangeClamped={DateRangeClamped}, PageSizeClamped={PageSizeClamped}",
                dateRangeClamped,
                pageSizeClamped);
        }
    }

    public void NoteForensicExportTruncation(bool truncated)
    {
        _pressureState.NoteForensicExportTruncation(truncated);
    }

    public async Task<OperationalResilienceSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var metrics = await GetMetricsSnapshotCachedAsync(cancellationToken);
        return BuildSummary(metrics);
    }

    public async Task<OperationalDegradedModesDto> GetDegradedModesAsync(CancellationToken cancellationToken = default)
    {
        var metrics = await GetMetricsSnapshotCachedAsync(cancellationToken);
        var primary = OperationalDegradedModeClassifier.ClassifyPrimary(metrics);
        var active = OperationalDegradedModeClassifier.ClassifyActiveModes(metrics);

        var modes = new[]
            {
                OperationalDegradedModeTypes.Normal,
                OperationalDegradedModeTypes.ElevatedQueryPressure,
                OperationalDegradedModeTypes.ReconciliationPressure,
                OperationalDegradedModeTypes.ExportPressure,
                OperationalDegradedModeTypes.AuditPersistencePressure,
                OperationalDegradedModeTypes.ReplayStormRisk
            }
            .Select(mode => new OperationalDegradedModeEntryDto
            {
                Mode = mode,
                Active = active.Contains(mode, StringComparer.Ordinal),
                SurvivabilityAssumption = OperationalResilienceGovernance.GetSurvivabilityAssumption(mode)
            })
            .ToList();

        _logger.LogInformation(
            "Operational resilience observability: degraded modes aggregated. Primary={Primary}, ActiveCount={ActiveCount}",
            primary,
            active.Count);

        return new OperationalDegradedModesDto
        {
            GeneratedAtUtc = metrics.GeneratedAtUtc,
            PrimaryDegradedMode = primary,
            Modes = modes
        };
    }

    public async Task<OperationalPressureIndicatorsDto> GetPressureIndicatorsAsync(
        CancellationToken cancellationToken = default)
    {
        var metrics = await GetMetricsSnapshotCachedAsync(cancellationToken);
        var indicators = OperationalPressureClassifier.BuildPressureIndicators(metrics);

        _logger.LogInformation(
            "Operational resilience observability: pressure indicators aggregated. ActiveIndicators={ActiveCount}",
            indicators.Count(i => i.Value));

        return new OperationalPressureIndicatorsDto
        {
            GeneratedAtUtc = metrics.GeneratedAtUtc,
            Indicators = indicators,
            Diagnostics = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["queryPressureExpectation"] = OperationalResilienceGovernance.GetQueryPressureExpectation(),
                ["forensicExportExpectation"] = OperationalResilienceGovernance.GetForensicExportPressureExpectation(),
                ["reconciliationAssumption"] = OperationalResilienceGovernance.GetReconciliationScalingAssumption(),
                ["auditSurvivability"] = OperationalResilienceGovernance.GetAuditSurvivabilityUnderStrain()
            }
        };
    }

    public async Task<OperationalReplayRiskSummaryDto> GetReplayRiskSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var metrics = await GetMetricsSnapshotCachedAsync(cancellationToken);
        var replayStorm = IsReplayStormRisk(metrics);

        var classification = replayStorm ? "Elevated" : metrics.ReplayMismatchUnresolvedCount > 0 ? "Advisory" : "Normal";

        if (replayStorm)
        {
            _logger.LogWarning(
                "Operational degraded mode: replay storm risk indicated. TotalReceipts={TotalReceipts}, MaxPerDevice={MaxPerDevice}",
                metrics.ReplayReceiptCount,
                metrics.MaxReplayReceiptsOnSingleDevice);
        }

        return new OperationalReplayRiskSummaryDto
        {
            GeneratedAtUtc = metrics.GeneratedAtUtc,
            TotalReplayReceiptCount = metrics.ReplayReceiptCount,
            MaxReceiptsOnSingleDevice = metrics.MaxReplayReceiptsOnSingleDevice,
            ReplayMismatchUnresolvedCount = metrics.ReplayMismatchUnresolvedCount,
            ReplayStormRiskIndicated = replayStorm,
            ReplayRiskClassification = classification,
            Guidance = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["replayStormThresholdTotal"] = OperationalResilienceConstants.ReplayStormReceiptCountThreshold.ToString(),
                ["replayStormThresholdPerDevice"] = OperationalResilienceConstants.ReplayStormDeviceReceiptThreshold.ToString(),
                ["note"] = "Visibility only; sync replay semantics unchanged; no automatic throttling."
            }
        };
    }

    private async Task<OperationalResilienceMetricsSnapshot> GetMetricsSnapshotCachedAsync(
        CancellationToken cancellationToken)
    {
        var bypass = EvaluateResilienceCacheBypass(out var bypassReason, out var degradedMode);

        if (bypass)
        {
            LogCachePressureEscalation(degradedMode, bypassReason);
        }

        var category = OperationalDiagnosticsCacheCategories.ResilienceMetrics;
        var pressureSignals = OperationalCacheAdaptivePressureSignalBuilder.ForResilience(
            _pressureState,
            _cache,
            IsReplayStormRisk);
        var effectiveTtl = OperationalCacheAdaptiveTtlHelper.ResolveEffectiveTtl(
            category,
            pressureSignals,
            _cache,
            _cacheTelemetry,
            _logger,
            out _);

        var envelope = await _cache.GetOrCreateAsync(
            OperationalDiagnosticsCacheConstants.ResilienceMetricsCacheKey,
            category,
            effectiveTtl,
            BuildMetricsAsync,
            bypass,
            cancellationToken).ConfigureAwait(false);

        return envelope.Value;
    }

    private bool EvaluateResilienceCacheBypass(out string bypassReason, out string degradedMode)
    {
        if (_pressureState.QueryDateRangeClamped || _pressureState.QueryPageSizeClamped)
        {
            bypassReason = "elevated query pressure";
            degradedMode = OperationalDegradedModeTypes.ElevatedQueryPressure;
            return true;
        }

        if (_pressureState.ForensicExportTruncated)
        {
            bypassReason = "export pressure";
            degradedMode = OperationalDegradedModeTypes.ExportPressure;
            return true;
        }

        if (_cache.TryGetEnvelope<OperationalResilienceMetricsSnapshot>(
                OperationalDiagnosticsCacheConstants.ResilienceMetricsCacheKey,
                OperationalDiagnosticsCacheCategories.ResilienceMetrics,
                out var cached)
            && cached != null
            && IsReplayStormRisk(cached.Value))
        {
            bypassReason = "replay storm risk";
            degradedMode = OperationalDegradedModeTypes.ReplayStormRisk;
            return true;
        }

        bypassReason = string.Empty;
        degradedMode = OperationalDegradedModeTypes.Normal;
        return false;
    }

    private void LogCachePressureEscalation(string degradedMode, string bypassReason)
    {
        _logger.LogWarning(
            "Operational cache pressure escalation: resilience metrics cache bypassed. Category={Category}, DegradedMode={DegradedMode}, BypassReason={BypassReason}",
            OperationalDiagnosticsCacheCategories.ResilienceMetrics,
            degradedMode,
            bypassReason);
    }

    private static bool IsReplayStormRisk(OperationalResilienceMetricsSnapshot metrics) =>
        metrics.MaxReplayReceiptsOnSingleDevice >= OperationalResilienceConstants.ReplayStormDeviceReceiptThreshold
        || metrics.ReplayReceiptCount >= OperationalResilienceConstants.ReplayStormReceiptCountThreshold;

    private OperationalResilienceSummaryDto BuildSummary(OperationalResilienceMetricsSnapshot metrics)
    {
        var primary = OperationalDegradedModeClassifier.ClassifyPrimary(metrics);

        if (primary != OperationalDegradedModeTypes.Normal)
        {
            _logger.LogWarning(
                "Operational degraded mode: primary mode active. Mode={Mode}, Unresolved={Unresolved}, AuditFailures={AuditFailures}",
                primary,
                metrics.UnresolvedConflictCount,
                metrics.RecentAuditPersistenceFailures);
        }
        else
        {
            _logger.LogInformation(
                "Operational resilience observability: resilience summary generated. Mode={Mode}",
                primary);
        }

        return new OperationalResilienceSummaryDto
        {
            GeneratedAtUtc = metrics.GeneratedAtUtc,
            PrimaryDegradedMode = primary,
            ActiveDegradedModes = OperationalDegradedModeClassifier.ClassifyActiveModes(metrics),
            ReconciliationBacklogSeverity = OperationalDegradedModeClassifier.ClassifyReconciliationBacklogSeverity(metrics),
            QueryPressureIndicated = metrics.QueryDateRangeClamped || metrics.QueryPageSizeClamped,
            ReplayStormRiskIndicated = IsReplayStormRisk(metrics),
            ExportTruncationPressureIndicated = metrics.ForensicExportTruncated || metrics.TruncationWarningsIndicated,
            AuditPersistencePressureIndicated = metrics.RecentAuditPersistenceFailures > 0,
            UnresolvedConflictCount = metrics.UnresolvedConflictCount,
            ReplayReceiptCount = metrics.ReplayReceiptCount,
            AuditRecordCount = metrics.AuditRecordCount,
            RecentAuditPersistenceFailures = metrics.RecentAuditPersistenceFailures,
            ResilienceGuidance = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["survivability"] = OperationalResilienceGovernance.GetSurvivabilityAssumption(primary),
                ["nonGoals"] = "no distributed circuit breakers; no queues; no autoscaling; no request shedding"
            }
        };
    }

    private async Task<OperationalResilienceMetricsSnapshot> BuildMetricsAsync(CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var advisoryCutoff = utcNow.AddDays(-OperationalRetentionConstants.UnresolvedAdvisoryDays);

        var unresolvedQuery = _db.SyncConflictRecords.AsNoTracking()
            .Where(r => UnresolvedStatuses.Contains(r.ResolutionStatus));

        var unresolvedCount = await unresolvedQuery.CountAsync(cancellationToken).ConfigureAwait(false);
        var over7 = await unresolvedQuery.CountAsync(r => r.CreatedAtUtc <= advisoryCutoff, cancellationToken).ConfigureAwait(false);
        var replayMismatchUnresolved = await unresolvedQuery.CountAsync(
            r => r.ConflictType.Contains("ReplayMismatch"),
            cancellationToken).ConfigureAwait(false);

        var auditCount = await _db.OperationalAuditRecords.AsNoTracking().CountAsync(cancellationToken).ConfigureAwait(false);
        var receiptCount = await _db.SyncOperationReceipts.AsNoTracking().CountAsync(cancellationToken).ConfigureAwait(false);

        var maxPerDevice = await _db.SyncOperationReceipts.AsNoTracking()
            .GroupBy(r => r.DeviceId)
            .Select(g => g.Count())
            .OrderByDescending(c => c)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        var truncationIndicated = over7 > 0 || auditCount > OperationalRetentionConstants.MaxTimelineExpansionItems;

        return new OperationalResilienceMetricsSnapshot
        {
            GeneratedAtUtc = utcNow,
            UnresolvedConflictCount = unresolvedCount,
            UnresolvedOver7DaysCount = over7,
            ReplayReceiptCount = receiptCount,
            MaxReplayReceiptsOnSingleDevice = maxPerDevice,
            AuditRecordCount = auditCount,
            ReplayMismatchUnresolvedCount = replayMismatchUnresolved,
            RecentAuditPersistenceFailures = _auditTelemetry.GetRecentFailureCount(),
            TruncationWarningsIndicated = truncationIndicated,
            QueryDateRangeClamped = _pressureState.QueryDateRangeClamped,
            QueryPageSizeClamped = _pressureState.QueryPageSizeClamped,
            ForensicExportTruncated = _pressureState.ForensicExportTruncated
        };
    }
}
