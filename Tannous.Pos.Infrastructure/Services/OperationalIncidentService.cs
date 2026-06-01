using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.OperationalComposition;
using Tannous.Pos.Application.OperationalDashboard;
using Tannous.Pos.Application.OperationalIncidents;
using Tannous.Pos.Application.OperationalInventoryWorkbench;
using Tannous.Pos.Application.OperationalRecovery;
using Tannous.Pos.Application.OperationalReplayWorkbench;
using Tannous.Pos.Application.OperationalTimeline;
using Tannous.Pos.Application.OperationalTrends;
using Tannous.Pos.Application.OperationalTriage;
using Tannous.Pos.Application.OperationalWorkbench;

namespace Tannous.Pos.Infrastructure.Services;

/// <summary>
/// Operator incident cases via composition hub and existing operational read services (no workbench service recursion).
/// </summary>
public sealed class OperationalIncidentService : IOperationalIncidentService
{
    private readonly IOperationalReadCompositionHub _compositionHub;
    private readonly IOperationalTrendService _trendService;
    private readonly IOperationalTimelineService _timelineService;
    private readonly IOperationalTriageService _triageService;
    private readonly IOperationalRecoveryService _recoveryService;
    private readonly IOperationalIncidentCaseStore _incidentCaseStore;
    private readonly ILogger<OperationalIncidentService> _logger;

    public OperationalIncidentService(
        IOperationalReadCompositionHub compositionHub,
        IOperationalTrendService trendService,
        IOperationalTimelineService timelineService,
        IOperationalTriageService triageService,
        IOperationalRecoveryService recoveryService,
        IOperationalIncidentCaseStore incidentCaseStore,
        ILogger<OperationalIncidentService> logger)
    {
        _compositionHub = compositionHub;
        _trendService = trendService;
        _timelineService = timelineService;
        _triageService = triageService;
        _recoveryService = recoveryService;
        _incidentCaseStore = incidentCaseStore;
        _logger = logger;
    }

    public async Task<OperationalIncidentCasesDto> GetIncidentCasesAsync(CancellationToken cancellationToken = default)
    {
        var context = await LoadIncidentContextAsync(cancellationToken).ConfigureAwait(false);
        var priorSnapshots = _incidentCaseStore.GetSnapshots();
        var generatedAtUtc = DateTime.UtcNow;

        var cases = OperationalIncidentAggregation.ComposeCases(
            context.Trend,
            context.Timeline,
            context.TimelineCorrelations,
            context.Triage,
            context.Recovery,
            context.Dashboard,
            context.ReplayPressure,
            context.ReplayStabilization,
            context.ReplayRecoveryConfidence,
            context.ReconciliationWorkbench,
            context.InventoryWorkbench,
            context.RuntimeProtection,
            context.Fingerprint,
            context.RuntimeSaturationIndicated,
            context.ProtectiveModeActive,
            priorSnapshots,
            generatedAtUtc);

        foreach (var snapshot in OperationalIncidentAggregation.CreateSnapshots(
                     cases.Cases,
                     context.Fingerprint,
                     generatedAtUtc))
        {
            _incidentCaseStore.Append(snapshot);
        }

        _logger.LogInformation(
            "Operational incident observability: cases composed. CaseCount={CaseCount}, RecurringCount={RecurringCount}",
            cases.CaseCount,
            cases.Cases.Count(c => c.IsRecurring));

        return cases;
    }

    public async Task<OperationalIncidentCasesSummaryDto> GetIncidentSummaryAsync(CancellationToken cancellationToken = default)
    {
        var context = await LoadIncidentContextAsync(cancellationToken).ConfigureAwait(false);
        var priorSnapshots = _incidentCaseStore.GetSnapshots();
        var generatedAtUtc = DateTime.UtcNow;

        var cases = OperationalIncidentAggregation.ComposeCases(
            context.Trend,
            context.Timeline,
            context.TimelineCorrelations,
            context.Triage,
            context.Recovery,
            context.Dashboard,
            context.ReplayPressure,
            context.ReplayStabilization,
            context.ReplayRecoveryConfidence,
            context.ReconciliationWorkbench,
            context.InventoryWorkbench,
            context.RuntimeProtection,
            context.Fingerprint,
            context.RuntimeSaturationIndicated,
            context.ProtectiveModeActive,
            priorSnapshots,
            generatedAtUtc);

        var summary = OperationalIncidentAggregation.ComposeSummary(cases.Cases, context.Recovery, generatedAtUtc);

        _logger.LogInformation(
            "Operational incident observability: summary composed. ActiveCount={ActiveCount}, HighestSeverity={HighestSeverity}",
            summary.ActiveIncidentCount,
            summary.HighestSeverity);

        return summary;
    }

    public async Task<OperationalIncidentCaseDetailDto?> GetIncidentDetailsAsync(
        string incidentId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(incidentId))
            return null;

        var context = await LoadIncidentContextAsync(cancellationToken).ConfigureAwait(false);
        var priorSnapshots = _incidentCaseStore.GetSnapshots();
        var generatedAtUtc = DateTime.UtcNow;

        var cases = OperationalIncidentAggregation.ComposeCases(
            context.Trend,
            context.Timeline,
            context.TimelineCorrelations,
            context.Triage,
            context.Recovery,
            context.Dashboard,
            context.ReplayPressure,
            context.ReplayStabilization,
            context.ReplayRecoveryConfidence,
            context.ReconciliationWorkbench,
            context.InventoryWorkbench,
            context.RuntimeProtection,
            context.Fingerprint,
            context.RuntimeSaturationIndicated,
            context.ProtectiveModeActive,
            priorSnapshots,
            generatedAtUtc);

        var details = OperationalIncidentAggregation.ComposeDetails(
            incidentId.Trim(),
            cases.Cases,
            context.Trend,
            context.Timeline,
            context.TimelineCorrelations,
            context.Triage,
            context.Recovery,
            context.Dashboard,
            context.ReplayPressure,
            context.ReplayStabilization,
            context.ReplayRecoveryConfidence,
            context.ReconciliationWorkbench,
            context.InventoryWorkbench,
            context.RuntimeProtection,
            context.Fingerprint,
            context.RuntimeSaturationIndicated,
            context.ProtectiveModeActive);

        if (details is null)
        {
            _logger.LogInformation(
                "Operational incident observability: details not found. IncidentId={IncidentId}",
                incidentId);
            return null;
        }

        _logger.LogInformation(
            "Operational incident observability: details composed. IncidentId={IncidentId}, SignalCount={SignalCount}",
            details.Case.IncidentId,
            details.Signals.Count);

        return details;
    }

    private async Task<OperationalIncidentContext> LoadIncidentContextAsync(CancellationToken cancellationToken)
    {
        var trend = await _trendService.GetSummaryAsync(cancellationToken).ConfigureAwait(false);
        var timeline = await _timelineService.GetTimelineAsync(cancellationToken).ConfigureAwait(false);
        var timelineCorrelations = await _timelineService.GetCorrelationsAsync(cancellationToken).ConfigureAwait(false);
        var triage = await _triageService.GetTriageQueueAsync(cancellationToken).ConfigureAwait(false);
        var recovery = await _recoveryService.GetRecoveryPostureAsync(cancellationToken).ConfigureAwait(false);

        await _compositionHub.BuildSnapshotAsync(cancellationToken).ConfigureAwait(false);

        var dashboard = await _compositionHub.GetDashboardSummaryAsync(cancellationToken).ConfigureAwait(false);
        var reconciliationWorkbench = await _compositionHub
            .GetReconciliationWorkbenchViewAsync(cancellationToken)
            .ConfigureAwait(false);
        var inventoryWorkbench = await _compositionHub.GetInventoryWorkbenchViewAsync(cancellationToken).ConfigureAwait(false);
        var runtimeProtection = await _compositionHub.GetRuntimeProtectionSnapshotAsync(cancellationToken).ConfigureAwait(false);
        var fingerprint = await _compositionHub.GetFingerprintSnapshotAsync(cancellationToken).ConfigureAwait(false);

        var resilience = await _compositionHub.GetResilienceSummaryAsync(cancellationToken).ConfigureAwait(false);
        var reconciliation = await _compositionHub.GetReconciliationSummaryAsync(cancellationToken).ConfigureAwait(false);
        var alerts = await _compositionHub.GetAlertSummaryAsync(cancellationToken).ConfigureAwait(false);
        var incidents = await _compositionHub.GetIncidentsSummaryAsync(cancellationToken).ConfigureAwait(false);
        var governanceOverview = await _compositionHub.GetGovernanceOverviewAsync(cancellationToken).ConfigureAwait(false);

        var runtimeSignals = new OperationalReplayRuntimeSignals
        {
            ProtectiveContainmentActive = runtimeProtection.FailsafeActive
                || dashboard.Pressure.ProtectiveModeActive,
            RuntimeSaturationIndicated = string.Equals(
                    runtimeProtection.TelemetrySaturationLevel,
                    "Elevated",
                    StringComparison.OrdinalIgnoreCase)
                || string.Equals(
                    runtimeProtection.TelemetrySaturationLevel,
                    "Saturated",
                    StringComparison.OrdinalIgnoreCase)
                || dashboard.Pressure.RuntimeSaturationIndicated
        };

        var replayPressure = OperationalReplayWorkbenchAggregation.ComposePressureSummary(
            resilience,
            reconciliation,
            alerts,
            incidents,
            dashboard,
            reconciliationWorkbench,
            runtimeSignals);

        var replayStabilization = OperationalReplayWorkbenchAggregation.ComposeStabilization(
            resilience,
            reconciliation,
            dashboard,
            reconciliationWorkbench,
            inventoryWorkbench,
            runtimeSignals,
            replayPressure);

        var replayRecoveryConfidence = OperationalReplayWorkbenchAggregation.ComposeRecoveryConfidence(
            resilience,
            dashboard,
            reconciliationWorkbench,
            replayStabilization,
            governanceOverview,
            runtimeSignals);

        var protectiveModeActive = dashboard.Pressure.ProtectiveModeActive
            || replayPressure.ProtectiveModeVisible
            || runtimeProtection.FailsafeActive;

        return new OperationalIncidentContext
        {
            Trend = trend,
            Timeline = timeline,
            TimelineCorrelations = timelineCorrelations,
            Triage = triage,
            Recovery = recovery,
            Dashboard = dashboard,
            ReconciliationWorkbench = reconciliationWorkbench,
            InventoryWorkbench = inventoryWorkbench,
            RuntimeProtection = runtimeProtection,
            Fingerprint = fingerprint,
            ReplayPressure = replayPressure,
            ReplayStabilization = replayStabilization,
            ReplayRecoveryConfidence = replayRecoveryConfidence,
            RuntimeSaturationIndicated = runtimeSignals.RuntimeSaturationIndicated,
            ProtectiveModeActive = protectiveModeActive
        };
    }

    private sealed class OperationalIncidentContext
    {
        public OperationalTrendSummaryDto Trend { get; init; } = new();
        public OperationalTimelineDto Timeline { get; init; } = new();
        public IReadOnlyList<OperationalTimelineCorrelationDto> TimelineCorrelations { get; init; } =
            Array.Empty<OperationalTimelineCorrelationDto>();
        public OperationalTriageQueueDto Triage { get; init; } = new();
        public OperationalRecoveryPostureDto Recovery { get; init; } = new();
        public OperationalDashboardSummaryDto Dashboard { get; init; } = new();
        public OperationalReconciliationWorkbenchDto ReconciliationWorkbench { get; init; } = new();
        public OperationalInventoryWorkbenchDto InventoryWorkbench { get; init; } = new();
        public OperationalGovernanceRuntimeProtectionSnapshot RuntimeProtection { get; init; } = new();
        public OperationalGovernanceFingerprintSnapshot Fingerprint { get; init; } = new();
        public OperationalReplayPressureSummaryDto ReplayPressure { get; init; } = new();
        public OperationalReplayStabilizationDto ReplayStabilization { get; init; } = new();
        public OperationalReplayRecoveryConfidenceDto ReplayRecoveryConfidence { get; init; } = new();
        public bool RuntimeSaturationIndicated { get; init; }
        public bool ProtectiveModeActive { get; init; }
    }
}
