using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Sync;

namespace Tannous.Pos.Infrastructure.Services;

/// <summary>
/// Query-time alert signals from existing diagnostics (no persistence; no external delivery).
/// GOVERNANCE / NON-GOAL: not an alerting provider; not guaranteed across restarts.
/// Composes from cached upstream summaries; optional alert-layer cache (signals + summary projections).
/// </summary>
public class OperationalAlertSignalService : IOperationalAlertSignalService
{
    private readonly IOperationalResilienceDiagnosticsService _resilience;
    private readonly IOperationalIncidentCorrelationService _incidents;
    private readonly ISyncConflictReconciliationService _reconciliation;
    private readonly IOperationalDiagnosticsCache _cache;
    private readonly IOperationalDiagnosticsCacheTelemetry _cacheTelemetry;
    private readonly IOperationalAuditPersistenceTelemetry _auditTelemetry;
    private readonly IOperationalResiliencePressureState _pressureState;
    private readonly ILogger<OperationalAlertSignalService> _logger;

    public OperationalAlertSignalService(
        IOperationalResilienceDiagnosticsService resilience,
        IOperationalIncidentCorrelationService incidents,
        ISyncConflictReconciliationService reconciliation,
        IOperationalDiagnosticsCache cache,
        IOperationalDiagnosticsCacheTelemetry cacheTelemetry,
        IOperationalAuditPersistenceTelemetry auditTelemetry,
        IOperationalResiliencePressureState pressureState,
        ILogger<OperationalAlertSignalService> logger)
    {
        _resilience = resilience;
        _incidents = incidents;
        _reconciliation = reconciliation;
        _cache = cache;
        _cacheTelemetry = cacheTelemetry;
        _auditTelemetry = auditTelemetry;
        _pressureState = pressureState;
        _logger = logger;
    }

    public Task<IReadOnlyList<OperationalAlertSignalDto>> GetCurrentSignalsAsync(
        CancellationToken cancellationToken = default) =>
        GetSignalsCachedAsync(cancellationToken);

    public async Task<IReadOnlyList<OperationalAlertSignalDto>> GetCriticalSignalsAsync(
        CancellationToken cancellationToken = default)
    {
        var signals = await GetSignalsCachedAsync(cancellationToken).ConfigureAwait(false);
        return signals
            .Where(s => s.Severity == OperationalAlertSeverity.Critical)
            .ToList();
    }

    public async Task<IReadOnlyList<OperationalAlertSignalDto>> GetReplayPressureSignalsAsync(
        CancellationToken cancellationToken = default)
    {
        var signals = await GetSignalsCachedAsync(cancellationToken).ConfigureAwait(false);
        return signals
            .Where(s =>
                s.AlertType == OperationalAlertTypes.ReplayStormRisk
                || s.Subsystems.Contains("Sync", StringComparer.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<IReadOnlyList<OperationalAlertSignalDto>> GetInventoryRiskSignalsAsync(
        CancellationToken cancellationToken = default)
    {
        var signals = await GetSignalsCachedAsync(cancellationToken).ConfigureAwait(false);
        return signals
            .Where(s =>
                s.AlertType == OperationalAlertTypes.InventoryDriftEscalation
                || s.Subsystems.Contains("Inventory", StringComparer.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<OperationalAlertSummaryDto> GetAlertSummaryAsync(CancellationToken cancellationToken = default)
    {
        var bypass = EvaluateAlertCacheBypass();
        if (_cache.TryGetEnvelope<OperationalAlertSummaryDto>(
                OperationalDiagnosticsCacheConstants.AlertSummaryCacheKey,
                OperationalDiagnosticsCacheCategories.AlertSummary,
                out _))
        {
            _logger.LogInformation(
                "Operational alert cache reuse: alert summary cache path engaged. Category={Category}",
                OperationalDiagnosticsCacheCategories.AlertSummary);
        }

        var summaryCategory = OperationalDiagnosticsCacheCategories.AlertSummary;
        var summaryTtl = ResolveAlertEffectiveTtl(summaryCategory);

        var envelope = await _cache.GetOrCreateAsync(
            OperationalDiagnosticsCacheConstants.AlertSummaryCacheKey,
            summaryCategory,
            summaryTtl,
            async ct =>
            {
                var signals = await GetSignalsCachedAsync(ct).ConfigureAwait(false);
                return BuildSummary(signals);
            },
            bypass,
            cancellationToken).ConfigureAwait(false);

        return envelope.Value;
    }

    private async Task<IReadOnlyList<OperationalAlertSignalDto>> GetSignalsCachedAsync(
        CancellationToken cancellationToken)
    {
        var bypass = EvaluateAlertCacheBypass();
        if (_cache.TryGetEnvelope<List<OperationalAlertSignalDto>>(
                OperationalDiagnosticsCacheConstants.AlertSignalsCacheKey,
                OperationalDiagnosticsCacheCategories.AlertSignals,
                out _))
        {
            _logger.LogInformation(
                "Operational alert cache reuse: alert signals cache path engaged. Category={Category}",
                OperationalDiagnosticsCacheCategories.AlertSignals);
        }

        var signalsCategory = OperationalDiagnosticsCacheCategories.AlertSignals;
        var signalsTtl = ResolveAlertEffectiveTtl(signalsCategory);

        var envelope = await _cache.GetOrCreateAsync(
            OperationalDiagnosticsCacheConstants.AlertSignalsCacheKey,
            signalsCategory,
            signalsTtl,
            BuildSignalsFromUpstreamAsync,
            bypass,
            cancellationToken).ConfigureAwait(false);

        return envelope.Value;
    }

    private bool EvaluateAlertCacheBypass() =>
        _pressureState.QueryDateRangeClamped
        || _pressureState.QueryPageSizeClamped
        || _pressureState.ForensicExportTruncated;

    private TimeSpan ResolveAlertEffectiveTtl(string category)
    {
        var pressureSignals = OperationalCacheAdaptivePressureSignalBuilder.ForIncident(
            _pressureState,
            _cache,
            static metrics =>
                metrics.MaxReplayReceiptsOnSingleDevice >= OperationalResilienceConstants.ReplayStormDeviceReceiptThreshold
                || metrics.ReplayReceiptCount >= OperationalResilienceConstants.ReplayStormReceiptCountThreshold);

        return OperationalCacheAdaptiveTtlHelper.ResolveEffectiveTtl(
            category,
            pressureSignals,
            _cache,
            _cacheTelemetry,
            _logger,
            out _);
    }

    private async Task<List<OperationalAlertSignalDto>> BuildSignalsFromUpstreamAsync(
        CancellationToken cancellationToken)
    {
        var generatedUtc = DateTime.UtcNow;
        var upstream = await LoadCachedUpstreamDiagnosticsAsync(cancellationToken).ConfigureAwait(false);

        var resilienceSummary = upstream.Resilience;
        var replayRisk = upstream.ReplayRisk;
        var reconciliation = upstream.Reconciliation;
        var incidentSummary = upstream.Incidents;

        var auditFailures = _auditTelemetry.GetRecentFailureCount();
        var incidentRisk = incidentSummary.OverallCorrelatedRisk;
        var signals = new List<OperationalAlertSignalDto>();

        if (replayRisk.ReplayStormRiskIndicated
            || resilienceSummary.ReplayStormRiskIndicated)
        {
            var severity = OperationalAlertSeverity.Critical;
            signals.Add(CreateSignal(
                OperationalAlertTypes.ReplayStormRisk,
                severity,
                $"Replay storm risk indicated (receipts={replayRisk.TotalReplayReceiptCount}, maxPerDevice={replayRisk.MaxReceiptsOnSingleDevice}).",
                new[] { "Sync", "Replay" },
                reconciliation.ReplayMismatchCount,
                generatedUtc,
                replayRisk.ReplayRiskClassification,
                incidentRisk));
        }
        else if (replayRisk.ReplayMismatchUnresolvedCount > 0
                 || reconciliation.ReplayMismatchCount > 0)
        {
            signals.Add(CreateSignal(
                OperationalAlertTypes.ReplayStormRisk,
                OperationalAlertSeverity.Warning,
                $"Replay mismatch conflicts present (unresolved={replayRisk.ReplayMismatchUnresolvedCount}, backlog={reconciliation.ReplayMismatchCount}).",
                new[] { "Sync", "Replay" },
                reconciliation.ReplayMismatchCount,
                generatedUtc,
                replayRisk.ReplayRiskClassification,
                incidentRisk));
        }

        if (auditFailures >= OperationalResilienceConstants.RecentAuditPersistenceFailureThreshold)
        {
            signals.Add(CreateSignal(
                OperationalAlertTypes.AuditPersistencePressure,
                OperationalAlertSeverity.Critical,
                $"Recent operational audit persistence failures={auditFailures} (threshold={OperationalResilienceConstants.RecentAuditPersistenceFailureThreshold}).",
                new[] { "Audit" },
                0,
                generatedUtc,
                "Elevated",
                incidentRisk));
        }
        else if (auditFailures > 0 || resilienceSummary.AuditPersistencePressureIndicated)
        {
            signals.Add(CreateSignal(
                OperationalAlertTypes.AuditPersistencePressure,
                OperationalAlertSeverity.Warning,
                $"Operational audit persistence pressure indicated (recentFailures={auditFailures}).",
                new[] { "Audit" },
                0,
                generatedUtc,
                "Advisory",
                incidentRisk));
        }

        if (reconciliation.InventoryDriftRiskCount >= OperationalAlertSignalConstants.InventoryDriftCriticalThreshold)
        {
            signals.Add(CreateSignal(
                OperationalAlertTypes.InventoryDriftEscalation,
                OperationalAlertSeverity.Critical,
                $"Inventory drift risk conflicts={reconciliation.InventoryDriftRiskCount}.",
                new[] { "Inventory", "Sync" },
                reconciliation.InventoryDriftRiskCount,
                generatedUtc,
                resilienceSummary.ReconciliationBacklogSeverity,
                incidentRisk));
        }
        else if (reconciliation.InventoryDriftRiskCount >= OperationalAlertSignalConstants.InventoryDriftWarningThreshold)
        {
            signals.Add(CreateSignal(
                OperationalAlertTypes.InventoryDriftEscalation,
                OperationalAlertSeverity.Warning,
                $"Inventory drift risk conflicts={reconciliation.InventoryDriftRiskCount}.",
                new[] { "Inventory", "Sync" },
                reconciliation.InventoryDriftRiskCount,
                generatedUtc,
                resilienceSummary.ReconciliationBacklogSeverity,
                incidentRisk));
        }

        if (incidentSummary.CascadingDegradationCount > 0
            || resilienceSummary.PrimaryDegradedMode == OperationalDegradedModeTypes.ReplayStormRisk)
        {
            var severity = incidentSummary.CascadingDegradationCount >= 2
                || incidentSummary.CriticalIncidentCount > 0
                ? OperationalAlertSeverity.Critical
                : OperationalAlertSeverity.Warning;

            signals.Add(CreateSignal(
                OperationalAlertTypes.CascadingOperationalPressure,
                severity,
                $"Cascading operational pressure (cascadingGroups={incidentSummary.CascadingDegradationCount}, incidentGroups={incidentSummary.TotalIncidentGroups}).",
                new[] { "Resilience", "Incident" },
                reconciliation.UnresolvedCount,
                generatedUtc,
                resilienceSummary.PrimaryDegradedMode,
                incidentRisk));
        }

        if (reconciliation.UnresolvedCount >= OperationalResilienceConstants.HighUnresolvedConflictThreshold)
        {
            signals.Add(CreateSignal(
                OperationalAlertTypes.ReconciliationBacklog,
                OperationalAlertSeverity.Critical,
                $"Reconciliation backlog critical (unresolved={reconciliation.UnresolvedCount}).",
                new[] { "Reconciliation", "Sync" },
                reconciliation.UnresolvedCount,
                generatedUtc,
                resilienceSummary.ReconciliationBacklogSeverity,
                incidentRisk));
        }
        else if (reconciliation.UnresolvedCount >= OperationalResilienceConstants.ReconciliationBacklogElevatedThreshold)
        {
            signals.Add(CreateSignal(
                OperationalAlertTypes.ReconciliationBacklog,
                OperationalAlertSeverity.Warning,
                $"Reconciliation backlog elevated (unresolved={reconciliation.UnresolvedCount}).",
                new[] { "Reconciliation", "Sync" },
                reconciliation.UnresolvedCount,
                generatedUtc,
                resilienceSummary.ReconciliationBacklogSeverity,
                incidentRisk));
        }

        if (reconciliation.UnresolvedCount >= OperationalAlertSignalConstants.ConflictEscalationCriticalThreshold)
        {
            signals.Add(CreateSignal(
                OperationalAlertTypes.ConflictEscalation,
                OperationalAlertSeverity.Critical,
                $"Unresolved conflict escalation (total={reconciliation.UnresolvedCount}).",
                new[] { "Sync", "Reconciliation" },
                reconciliation.UnresolvedCount,
                generatedUtc,
                resilienceSummary.ReconciliationBacklogSeverity,
                incidentRisk));
        }
        else if (reconciliation.UnresolvedCount >= OperationalAlertSignalConstants.ConflictEscalationWarningThreshold)
        {
            signals.Add(CreateSignal(
                OperationalAlertTypes.ConflictEscalation,
                OperationalAlertSeverity.Warning,
                $"Unresolved conflict count elevated (total={reconciliation.UnresolvedCount}).",
                new[] { "Sync", "Reconciliation" },
                reconciliation.UnresolvedCount,
                generatedUtc,
                resilienceSummary.ReconciliationBacklogSeverity,
                incidentRisk));
        }

        if (resilienceSummary.ExportTruncationPressureIndicated
            || _pressureState.ForensicExportTruncated)
        {
            signals.Add(CreateSignal(
                OperationalAlertTypes.ExportTruncationPressure,
                OperationalAlertSeverity.Warning,
                "Forensic export truncation or export pressure indicated in current process.",
                new[] { "Forensic", "Export" },
                0,
                generatedUtc,
                OperationalResilienceGovernance.GetForensicExportPressureExpectation(),
                incidentRisk));
        }

        if (reconciliation.LifecycleConflictCount >= OperationalAlertSignalConstants.LifecycleConflictCriticalThreshold)
        {
            signals.Add(CreateSignal(
                OperationalAlertTypes.LifecycleConflictSpike,
                OperationalAlertSeverity.Critical,
                $"Lifecycle conflict spike (count={reconciliation.LifecycleConflictCount}).",
                new[] { "Order", "Lifecycle" },
                reconciliation.LifecycleConflictCount,
                generatedUtc,
                resilienceSummary.ReconciliationBacklogSeverity,
                incidentRisk));
        }
        else if (reconciliation.LifecycleConflictCount >= OperationalAlertSignalConstants.LifecycleConflictWarningThreshold)
        {
            signals.Add(CreateSignal(
                OperationalAlertTypes.LifecycleConflictSpike,
                OperationalAlertSeverity.Warning,
                $"Lifecycle conflicts elevated (count={reconciliation.LifecycleConflictCount}).",
                new[] { "Order", "Lifecycle" },
                reconciliation.LifecycleConflictCount,
                generatedUtc,
                resilienceSummary.ReconciliationBacklogSeverity,
                incidentRisk));
        }

        foreach (var signal in signals)
        {
            _logger.LogInformation(
                "Operational alert visibility: signal derived. AlertType={AlertType}, Severity={Severity}, RelatedConflictCount={RelatedConflictCount}, PressureClassification={PressureClassification}",
                signal.AlertType,
                signal.Severity,
                signal.RelatedConflictCount,
                signal.PressureClassification);

            if (signal.Severity == OperationalAlertSeverity.Critical)
            {
                _logger.LogWarning(
                    "Operational escalation visibility: critical alert signal active. AlertType={AlertType}, Summary={Summary}",
                    signal.AlertType,
                    signal.Summary);
            }

            if (signal.AlertType is OperationalAlertTypes.ReplayStormRisk
                or OperationalAlertTypes.ExportTruncationPressure
                or OperationalAlertTypes.CascadingOperationalPressure)
            {
                _logger.LogWarning(
                    "Operational pressure escalation: pressure-classified alert. AlertType={AlertType}, PressureClassification={PressureClassification}",
                    signal.AlertType,
                    signal.PressureClassification);
            }
        }

        _logger.LogInformation(
            "Operational alert visibility: alert signal aggregation complete. TotalSignals={TotalSignals}, Critical={Critical}, Warning={Warning}",
            signals.Count,
            signals.Count(s => s.Severity == OperationalAlertSeverity.Critical),
            signals.Count(s => s.Severity == OperationalAlertSeverity.Warning));

        return signals;
    }

    /// <summary>
    /// Sequential composition over cached upstream diagnostics services (no alert-layer cache).
    /// </summary>
    private async Task<CachedUpstreamDiagnostics> LoadCachedUpstreamDiagnosticsAsync(
        CancellationToken cancellationToken)
    {
        var resilience = await _resilience.GetSummaryAsync(cancellationToken).ConfigureAwait(false);
        var replayRisk = await _resilience.GetReplayRiskSummaryAsync(cancellationToken).ConfigureAwait(false);
        var reconciliation = await _reconciliation.GetSummaryAsync(cancellationToken).ConfigureAwait(false);
        var incidents = await _incidents.GetSummaryAsync(cancellationToken).ConfigureAwait(false);

        return new CachedUpstreamDiagnostics(resilience, replayRisk, reconciliation, incidents);
    }

    private readonly record struct CachedUpstreamDiagnostics(
        OperationalResilienceSummaryDto Resilience,
        OperationalReplayRiskSummaryDto ReplayRisk,
        ReconciliationSummaryDto Reconciliation,
        OperationalIncidentSummaryDto Incidents);

    private OperationalAlertSignalDto CreateSignal(
        string alertType,
        string severity,
        string summary,
        IReadOnlyList<string> subsystems,
        int relatedConflictCount,
        DateTime generatedUtc,
        string pressureClassification,
        string incidentRisk) =>
        new()
        {
            AlertType = alertType,
            Severity = severity,
            Summary = summary,
            Subsystems = subsystems,
            RelatedConflictCount = relatedConflictCount,
            GeneratedAtUtc = generatedUtc,
            EscalationRecommendation = OperationalAlertGovernance.GetEscalationRecommendation(alertType, severity),
            PressureClassification = pressureClassification,
            IncidentRisk = incidentRisk,
            SuggestedOperatorAction = OperationalAlertGovernance.GetSuggestedOperatorAction(alertType)
        };

    private static OperationalAlertSummaryDto BuildSummary(IReadOnlyList<OperationalAlertSignalDto> signals) =>
        new()
        {
            GeneratedAtUtc = DateTime.UtcNow,
            TotalSignals = signals.Count,
            CriticalSignals = signals.Count(s => s.Severity == OperationalAlertSeverity.Critical),
            WarningSignals = signals.Count(s => s.Severity == OperationalAlertSeverity.Warning),
            ReplayRelatedSignals = signals.Count(s =>
                s.AlertType == OperationalAlertTypes.ReplayStormRisk),
            InventoryRelatedSignals = signals.Count(s =>
                s.AlertType == OperationalAlertTypes.InventoryDriftEscalation)
        };
}
