using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.OperationalCausality;
using Tannous.Pos.Application.OperationalComposition;
using Tannous.Pos.Application.OperationalDashboard;
using Tannous.Pos.Application.OperationalIncidents;
using Tannous.Pos.Application.OperationalInventoryWorkbench;
using Tannous.Pos.Application.OperationalPatterns;
using Tannous.Pos.Application.OperationalPlaybooks;
using Tannous.Pos.Application.OperationalRecovery;
using Tannous.Pos.Application.OperationalReplayWorkbench;
using Tannous.Pos.Application.OperationalSimulation;
using Tannous.Pos.Application.OperationalSituationRoom;
using Tannous.Pos.Application.OperationalTimeline;
using Tannous.Pos.Application.OperationalTrends;
using Tannous.Pos.Application.OperationalTriage;
using Tannous.Pos.Application.OperationalWorkbench;

namespace Tannous.Pos.Infrastructure.Services;

/// <summary>
/// Operator pattern intelligence via composition hub and existing operational read services (no workbench/pattern service recursion).
/// </summary>
public sealed class OperationalPatternService : IOperationalPatternService
{
    private readonly IOperationalReadCompositionHub _compositionHub;
    private readonly IOperationalTrendService _trendService;
    private readonly IOperationalTimelineService _timelineService;
    private readonly IOperationalTriageService _triageService;
    private readonly IOperationalRecoveryService _recoveryService;
    private readonly IOperationalIncidentCaseStore _incidentCaseStore;
    private readonly IOperationalCausalitySnapshotStore _causalitySnapshotStore;
    private readonly IOperationalSituationSnapshotStore _situationSnapshotStore;
    private readonly IOperationalSimulationSnapshotStore _simulationSnapshotStore;
    private readonly IOperationalPlaybookSnapshotStore _playbookSnapshotStore;
    private readonly IOperationalPatternSnapshotStore _patternSnapshotStore;
    private readonly ILogger<OperationalPatternService> _logger;

    public OperationalPatternService(
        IOperationalReadCompositionHub compositionHub,
        IOperationalTrendService trendService,
        IOperationalTimelineService timelineService,
        IOperationalTriageService triageService,
        IOperationalRecoveryService recoveryService,
        IOperationalIncidentCaseStore incidentCaseStore,
        IOperationalCausalitySnapshotStore causalitySnapshotStore,
        IOperationalSituationSnapshotStore situationSnapshotStore,
        IOperationalSimulationSnapshotStore simulationSnapshotStore,
        IOperationalPlaybookSnapshotStore playbookSnapshotStore,
        IOperationalPatternSnapshotStore patternSnapshotStore,
        ILogger<OperationalPatternService> logger)
    {
        _compositionHub = compositionHub;
        _trendService = trendService;
        _timelineService = timelineService;
        _triageService = triageService;
        _recoveryService = recoveryService;
        _incidentCaseStore = incidentCaseStore;
        _causalitySnapshotStore = causalitySnapshotStore;
        _situationSnapshotStore = situationSnapshotStore;
        _simulationSnapshotStore = simulationSnapshotStore;
        _playbookSnapshotStore = playbookSnapshotStore;
        _patternSnapshotStore = patternSnapshotStore;
        _logger = logger;
    }

    public async Task<OperationalPatternsDto> GetOperationalPatternsAsync(CancellationToken cancellationToken = default)
    {
        var context = await LoadPatternContextAsync(cancellationToken).ConfigureAwait(false);
        var priorSnapshots = _patternSnapshotStore.GetSnapshots();
        var generatedAtUtc = DateTime.UtcNow;

        var patterns = OperationalPatternAggregation.ComposePatterns(
            context.Trend,
            context.Recovery,
            context.IncidentSummary,
            context.CausalitySummary,
            context.Propagation,
            context.Chains,
            context.SituationRoom,
            context.Simulation,
            context.Playbooks,
            priorSnapshots,
            generatedAtUtc);

        var archetypes = OperationalPatternAggregation.ComposeArchetypesResponse(
            context.Trend,
            context.Recovery,
            context.IncidentSummary,
            context.CausalitySummary,
            context.Propagation,
            context.SituationRoom,
            context.Simulation,
            context.Playbooks,
            priorSnapshots,
            generatedAtUtc);

        var summary = OperationalPatternAggregation.ComposeSummary(
            patterns,
            archetypes,
            context.SituationRoom,
            generatedAtUtc);

        _patternSnapshotStore.Append(OperationalPatternAggregation.CreateSnapshot(patterns, summary, archetypes));

        _logger.LogInformation(
            "Operational pattern observability: patterns composed. PatternCount={PatternCount}, SequenceCount={SequenceCount}",
            patterns.PatternCount,
            patterns.SequenceCount);

        return patterns;
    }

    public async Task<OperationalPatternSummaryDto> GetPatternSummaryAsync(CancellationToken cancellationToken = default)
    {
        var context = await LoadPatternContextAsync(cancellationToken).ConfigureAwait(false);
        var generatedAtUtc = DateTime.UtcNow;

        var patterns = OperationalPatternAggregation.ComposePatterns(
            context.Trend,
            context.Recovery,
            context.IncidentSummary,
            context.CausalitySummary,
            context.Propagation,
            context.Chains,
            context.SituationRoom,
            context.Simulation,
            context.Playbooks,
            _patternSnapshotStore.GetSnapshots(),
            generatedAtUtc);

        var archetypes = OperationalPatternAggregation.ComposeArchetypesResponse(
            context.Trend,
            context.Recovery,
            context.IncidentSummary,
            context.CausalitySummary,
            context.Propagation,
            context.SituationRoom,
            context.Simulation,
            context.Playbooks,
            _patternSnapshotStore.GetSnapshots(),
            generatedAtUtc);

        var summary = OperationalPatternAggregation.ComposeSummary(
            patterns,
            archetypes,
            context.SituationRoom,
            generatedAtUtc);

        _logger.LogInformation(
            "Operational pattern observability: summary composed. RecurringPatterns={RecurringPatterns}, DominantArchetype={DominantArchetype}",
            summary.RecurringPatternCount,
            summary.DominantArchetype);

        return summary;
    }

    public async Task<OperationalStabilizationArchetypesDto> GetStabilizationArchetypesAsync(
        CancellationToken cancellationToken = default)
    {
        var context = await LoadPatternContextAsync(cancellationToken).ConfigureAwait(false);
        var generatedAtUtc = DateTime.UtcNow;

        var archetypes = OperationalPatternAggregation.ComposeArchetypesResponse(
            context.Trend,
            context.Recovery,
            context.IncidentSummary,
            context.CausalitySummary,
            context.Propagation,
            context.SituationRoom,
            context.Simulation,
            context.Playbooks,
            _patternSnapshotStore.GetSnapshots(),
            generatedAtUtc);

        _logger.LogInformation(
            "Operational pattern observability: archetypes composed. ArchetypeCount={ArchetypeCount}",
            archetypes.ArchetypeCount);

        return archetypes;
    }

    private async Task<OperationalPatternContext> LoadPatternContextAsync(CancellationToken cancellationToken)
    {
        var trend = await _trendService.GetSummaryAsync(cancellationToken).ConfigureAwait(false);
        var timeline = await _timelineService.GetTimelineAsync(cancellationToken).ConfigureAwait(false);
        var timelineCorrelations = await _timelineService.GetCorrelationsAsync(cancellationToken).ConfigureAwait(false);
        var triage = await _triageService.GetTriageQueueAsync(cancellationToken).ConfigureAwait(false);
        var recovery = await _recoveryService.GetRecoveryPostureAsync(cancellationToken).ConfigureAwait(false);
        var recoveryOutlook = await _recoveryService.GetRecoveryOutlookAsync(cancellationToken).ConfigureAwait(false);

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
        var replayRecoveryConfidence = OperationalReplayWorkbenchAggregation.ComposeRecoveryConfidence(
            resilience,
            dashboard,
            reconciliationWorkbench,
            replayStabilization,
            governanceOverview,
            runtimeSignals);

        var incidentCases = OperationalIncidentAggregation.ComposeCases(
            trend,
            timeline,
            timelineCorrelations,
            triage,
            recovery,
            dashboard,
            replayPressure,
            replayStabilization,
            replayRecoveryConfidence,
            reconciliationWorkbench,
            inventoryWorkbench,
            runtimeProtection,
            fingerprint,
            runtimeSignals.RuntimeSaturationIndicated,
            protectiveModeActive,
            incidentSnapshots,
            generatedAtUtc);

        var incidentSummary = OperationalIncidentAggregation.ComposeSummary(
            incidentCases.Cases,
            recovery,
            generatedAtUtc);

        var priorCausalitySnapshots = _causalitySnapshotStore.GetSnapshots();
        var chains = OperationalCausalityAggregation.ComposeChains(
            trend,
            timeline,
            timelineCorrelations,
            triage,
            recovery,
            incidentCases.Cases,
            replayPressure,
            replayStabilization,
            reconciliationWorkbench,
            inventoryWorkbench,
            runtimeProtection,
            fingerprint,
            runtimeSignals.RuntimeSaturationIndicated,
            protectiveModeActive,
            priorCausalitySnapshots,
            generatedAtUtc);

        var propagation = OperationalCausalityAggregation.ComposePropagationAnalysis(
            trend,
            timeline,
            timelineCorrelations,
            triage,
            recovery,
            incidentCases.Cases,
            replayPressure,
            replayStabilization,
            reconciliationWorkbench,
            inventoryWorkbench,
            runtimeProtection,
            fingerprint,
            runtimeSignals.RuntimeSaturationIndicated,
            protectiveModeActive);

        var causalitySummary = OperationalCausalityAggregation.ComposeSummary(
            chains,
            propagation,
            recovery,
            generatedAtUtc);

        var priorSituationSnapshots = _situationSnapshotStore.GetSnapshots();
        var situationRoom = OperationalSituationRoomAggregation.ComposeSituationRoom(
            dashboard,
            trend,
            triage,
            recovery,
            recoveryOutlook,
            incidentSummary,
            causalitySummary,
            propagation,
            chains,
            priorSituationSnapshots,
            generatedAtUtc);

        var priorSimulationSnapshots = _simulationSnapshotStore.GetSnapshots();
        var simulation = OperationalSimulationAggregation.ComposeScenarios(
            dashboard,
            trend,
            recovery,
            recoveryOutlook,
            incidentSummary,
            causalitySummary,
            propagation,
            chains,
            situationRoom,
            priorSimulationSnapshots,
            generatedAtUtc);

        var priorPlaybookSnapshots = _playbookSnapshotStore.GetSnapshots();
        var playbooks = OperationalPlaybookAggregation.ComposePlaybooks(
            dashboard,
            triage,
            recovery,
            recoveryOutlook,
            incidentSummary,
            causalitySummary,
            propagation,
            situationRoom,
            simulation,
            priorPlaybookSnapshots,
            generatedAtUtc);

        return new OperationalPatternContext
        {
            Trend = trend,
            Recovery = recovery,
            IncidentSummary = incidentSummary,
            CausalitySummary = causalitySummary,
            Propagation = propagation,
            Chains = chains,
            SituationRoom = situationRoom,
            Simulation = simulation,
            Playbooks = playbooks
        };
    }

    private sealed class OperationalPatternContext
    {
        public OperationalTrendSummaryDto Trend { get; init; } = new();
        public OperationalRecoveryPostureDto Recovery { get; init; } = new();
        public OperationalIncidentCasesSummaryDto IncidentSummary { get; init; } = new();
        public OperationalCausalitySummaryDto CausalitySummary { get; init; } = new();
        public OperationalPropagationAnalysisDto Propagation { get; init; } = new();
        public OperationalCausalChainsDto Chains { get; init; } = new();
        public OperationalSituationRoomDto SituationRoom { get; init; } = new();
        public OperationalSimulationScenariosDto Simulation { get; init; } = new();
        public OperationalPlaybooksDto Playbooks { get; init; } = new();
    }
}
