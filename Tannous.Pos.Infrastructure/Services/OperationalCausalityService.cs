using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.OperationalComposition;
using Tannous.Pos.Application.OperationalCausality;
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
/// Operator causality interpretation via composition hub and existing operational read services (no workbench recursion).
/// </summary>
public sealed class OperationalCausalityService : IOperationalCausalityService
{
    private readonly IOperationalReadCompositionHub _compositionHub;
    private readonly IOperationalTrendService _trendService;
    private readonly IOperationalTimelineService _timelineService;
    private readonly IOperationalTriageService _triageService;
    private readonly IOperationalRecoveryService _recoveryService;
    private readonly IOperationalIncidentCaseStore _incidentCaseStore;
    private readonly IOperationalCausalitySnapshotStore _causalitySnapshotStore;
    private readonly ILogger<OperationalCausalityService> _logger;

    public OperationalCausalityService(
        IOperationalReadCompositionHub compositionHub,
        IOperationalTrendService trendService,
        IOperationalTimelineService timelineService,
        IOperationalTriageService triageService,
        IOperationalRecoveryService recoveryService,
        IOperationalIncidentCaseStore incidentCaseStore,
        IOperationalCausalitySnapshotStore causalitySnapshotStore,
        ILogger<OperationalCausalityService> logger)
    {
        _compositionHub = compositionHub;
        _trendService = trendService;
        _timelineService = timelineService;
        _triageService = triageService;
        _recoveryService = recoveryService;
        _incidentCaseStore = incidentCaseStore;
        _causalitySnapshotStore = causalitySnapshotStore;
        _logger = logger;
    }

    public async Task<OperationalCausalChainsDto> GetCausalChainsAsync(CancellationToken cancellationToken = default)
    {
        var context = await LoadCausalityContextAsync(cancellationToken).ConfigureAwait(false);
        var priorSnapshots = _causalitySnapshotStore.GetSnapshots();
        var generatedAtUtc = DateTime.UtcNow;

        var chains = OperationalCausalityAggregation.ComposeChains(
            context.Trend,
            context.Timeline,
            context.TimelineCorrelations,
            context.Triage,
            context.Recovery,
            context.Incidents,
            context.ReplayPressure,
            context.ReplayStabilization,
            context.ReconciliationWorkbench,
            context.InventoryWorkbench,
            context.RuntimeProtection,
            context.Fingerprint,
            context.RuntimeSaturationIndicated,
            context.ProtectiveModeActive,
            priorSnapshots,
            generatedAtUtc);

        _causalitySnapshotStore.Append(
            OperationalCausalityAggregation.CreateSnapshot(chains, generatedAtUtc));

        _logger.LogInformation(
            "Operational causality observability: chains composed. ChainCount={ChainCount}, NodeCount={NodeCount}",
            chains.ChainCount,
            chains.Nodes.Count);

        return chains;
    }

    public async Task<OperationalCausalitySummaryDto> GetCausalitySummaryAsync(CancellationToken cancellationToken = default)
    {
        var context = await LoadCausalityContextAsync(cancellationToken).ConfigureAwait(false);
        var priorSnapshots = _causalitySnapshotStore.GetSnapshots();
        var generatedAtUtc = DateTime.UtcNow;

        var chains = OperationalCausalityAggregation.ComposeChains(
            context.Trend,
            context.Timeline,
            context.TimelineCorrelations,
            context.Triage,
            context.Recovery,
            context.Incidents,
            context.ReplayPressure,
            context.ReplayStabilization,
            context.ReconciliationWorkbench,
            context.InventoryWorkbench,
            context.RuntimeProtection,
            context.Fingerprint,
            context.RuntimeSaturationIndicated,
            context.ProtectiveModeActive,
            priorSnapshots,
            generatedAtUtc);

        var propagation = OperationalCausalityAggregation.ComposePropagationAnalysis(
            context.Trend,
            context.Timeline,
            context.TimelineCorrelations,
            context.Triage,
            context.Recovery,
            context.Incidents,
            context.ReplayPressure,
            context.ReplayStabilization,
            context.ReconciliationWorkbench,
            context.InventoryWorkbench,
            context.RuntimeProtection,
            context.Fingerprint,
            context.RuntimeSaturationIndicated,
            context.ProtectiveModeActive);

        var summary = OperationalCausalityAggregation.ComposeSummary(chains, propagation, context.Recovery, generatedAtUtc);

        _logger.LogInformation(
            "Operational causality observability: summary composed. ActiveChains={ActiveChains}, Blockers={Blockers}",
            summary.ActiveCausalChains,
            summary.StabilizationBlockerCount);

        return summary;
    }

    public async Task<OperationalPropagationAnalysisDto> GetPropagationAnalysisAsync(
        CancellationToken cancellationToken = default)
    {
        var context = await LoadCausalityContextAsync(cancellationToken).ConfigureAwait(false);

        var analysis = OperationalCausalityAggregation.ComposePropagationAnalysis(
            context.Trend,
            context.Timeline,
            context.TimelineCorrelations,
            context.Triage,
            context.Recovery,
            context.Incidents,
            context.ReplayPressure,
            context.ReplayStabilization,
            context.ReconciliationWorkbench,
            context.InventoryWorkbench,
            context.RuntimeProtection,
            context.Fingerprint,
            context.RuntimeSaturationIndicated,
            context.ProtectiveModeActive);

        _logger.LogInformation(
            "Operational causality observability: propagation composed. PropagationCount={PropagationCount}, RootCauseCount={RootCauseCount}",
            analysis.PropagationCount,
            analysis.RootCauseCandidateCount);

        return analysis;
    }

    private async Task<OperationalCausalityContext> LoadCausalityContextAsync(CancellationToken cancellationToken)
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
        var incidentsSummary = await _compositionHub.GetIncidentsSummaryAsync(cancellationToken).ConfigureAwait(false);
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
            incidentsSummary,
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

        var protectiveModeActive = dashboard.Pressure.ProtectiveModeActive
            || replayPressure.ProtectiveModeVisible
            || runtimeProtection.FailsafeActive;

        var incidentSnapshots = _incidentCaseStore.GetSnapshots();
        var generatedAtUtc = DateTime.UtcNow;
        var incidentCases = OperationalIncidentAggregation.ComposeCases(
            trend,
            timeline,
            timelineCorrelations,
            triage,
            recovery,
            dashboard,
            replayPressure,
            replayStabilization,
            OperationalReplayWorkbenchAggregation.ComposeRecoveryConfidence(
                resilience,
                dashboard,
                reconciliationWorkbench,
                replayStabilization,
                governanceOverview,
                runtimeSignals),
            reconciliationWorkbench,
            inventoryWorkbench,
            runtimeProtection,
            fingerprint,
            runtimeSignals.RuntimeSaturationIndicated,
            protectiveModeActive,
            incidentSnapshots,
            generatedAtUtc);

        return new OperationalCausalityContext
        {
            Trend = trend,
            Timeline = timeline,
            TimelineCorrelations = timelineCorrelations,
            Triage = triage,
            Recovery = recovery,
            Incidents = incidentCases.Cases,
            ReconciliationWorkbench = reconciliationWorkbench,
            InventoryWorkbench = inventoryWorkbench,
            RuntimeProtection = runtimeProtection,
            Fingerprint = fingerprint,
            ReplayPressure = replayPressure,
            ReplayStabilization = replayStabilization,
            RuntimeSaturationIndicated = runtimeSignals.RuntimeSaturationIndicated,
            ProtectiveModeActive = protectiveModeActive
        };
    }

    private sealed class OperationalCausalityContext
    {
        public OperationalTrendSummaryDto Trend { get; init; } = new();
        public OperationalTimelineDto Timeline { get; init; } = new();
        public IReadOnlyList<OperationalTimelineCorrelationDto> TimelineCorrelations { get; init; } =
            Array.Empty<OperationalTimelineCorrelationDto>();
        public OperationalTriageQueueDto Triage { get; init; } = new();
        public OperationalRecoveryPostureDto Recovery { get; init; } = new();
        public IReadOnlyList<OperationalIncidentCaseDto> Incidents { get; init; } = Array.Empty<OperationalIncidentCaseDto>();
        public OperationalReconciliationWorkbenchDto ReconciliationWorkbench { get; init; } = new();
        public OperationalInventoryWorkbenchDto InventoryWorkbench { get; init; } = new();
        public OperationalGovernanceRuntimeProtectionSnapshot RuntimeProtection { get; init; } = new();
        public OperationalGovernanceFingerprintSnapshot Fingerprint { get; init; } = new();
        public OperationalReplayPressureSummaryDto ReplayPressure { get; init; } = new();
        public OperationalReplayStabilizationDto ReplayStabilization { get; init; } = new();
        public bool RuntimeSaturationIndicated { get; init; }
        public bool ProtectiveModeActive { get; init; }
    }
}
