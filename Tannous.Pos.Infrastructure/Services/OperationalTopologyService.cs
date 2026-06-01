using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.OperationalCausality;
using Tannous.Pos.Application.OperationalComposition;
using Tannous.Pos.Application.OperationalDashboard;
using Tannous.Pos.Application.OperationalDigest;
using Tannous.Pos.Application.OperationalEvolution;
using Tannous.Pos.Application.OperationalExperienceGraph;
using Tannous.Pos.Application.OperationalIncidents;
using Tannous.Pos.Application.OperationalIntegrity;
using Tannous.Pos.Application.OperationalInventoryWorkbench;
using Tannous.Pos.Application.OperationalPatterns;
using Tannous.Pos.Application.OperationalPlaybooks;
using Tannous.Pos.Application.OperationalRecovery;
using Tannous.Pos.Application.OperationalReplayWorkbench;
using Tannous.Pos.Application.OperationalSimulation;
using Tannous.Pos.Application.OperationalSituationRoom;
using Tannous.Pos.Application.OperationalTimeline;
using Tannous.Pos.Application.OperationalTopology;
using Tannous.Pos.Application.OperationalTrends;
using Tannous.Pos.Application.OperationalTriage;
using Tannous.Pos.Application.OperationalWorkbench;

namespace Tannous.Pos.Infrastructure.Services;

/// <summary>
/// Operator topology intelligence via composition hub and existing operational read services (no topology/evolution service recursion).
/// </summary>
public sealed class OperationalTopologyService : IOperationalTopologyService
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
    private readonly IOperationalIntegritySnapshotStore _integritySnapshotStore;
    private readonly IOperationalExperienceSnapshotStore _experienceSnapshotStore;
    private readonly IOperationalDigestSnapshotStore _digestSnapshotStore;
    private readonly IOperationalEvolutionSnapshotStore _evolutionSnapshotStore;
    private readonly IOperationalTopologySnapshotStore _topologySnapshotStore;
    private readonly ILogger<OperationalTopologyService> _logger;

    public OperationalTopologyService(
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
        IOperationalIntegritySnapshotStore integritySnapshotStore,
        IOperationalExperienceSnapshotStore experienceSnapshotStore,
        IOperationalDigestSnapshotStore digestSnapshotStore,
        IOperationalEvolutionSnapshotStore evolutionSnapshotStore,
        IOperationalTopologySnapshotStore topologySnapshotStore,
        ILogger<OperationalTopologyService> logger)
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
        _integritySnapshotStore = integritySnapshotStore;
        _experienceSnapshotStore = experienceSnapshotStore;
        _digestSnapshotStore = digestSnapshotStore;
        _evolutionSnapshotStore = evolutionSnapshotStore;
        _topologySnapshotStore = topologySnapshotStore;
        _logger = logger;
    }

    public async Task<OperationalTopologyDto> GetOperationalTopologyAsync(
        CancellationToken cancellationToken = default)
    {
        var context = await LoadTopologyContextAsync(cancellationToken).ConfigureAwait(false);
        var priorTopologySnapshots = _topologySnapshotStore.GetSnapshots();
        var generatedAtUtc = DateTime.UtcNow;

        var topology = OperationalTopologyAggregation.ComposeOperationalTopology(
            context.Recovery,
            context.CausalitySummary,
            context.Propagation,
            context.Chains,
            context.SituationRoom,
            context.SimulationSummary,
            context.Playbooks,
            context.PatternSummary,
            context.EvolutionTimeline,
            context.IntegrityReport,
            context.ExperienceGraph,
            context.Digest,
            priorTopologySnapshots,
            generatedAtUtc);

        _topologySnapshotStore.Append(OperationalTopologyAggregation.CreateSnapshot(topology));

        _logger.LogInformation(
            "Operational topology observability: topology composed. State={TopologyState}, DependencyCount={DependencyCount}",
            topology.TopologyState,
            topology.Dependencies.Count);

        return topology;
    }

    public async Task<OperationalTopologySummaryDto> GetTopologySummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var context = await LoadTopologyContextAsync(cancellationToken).ConfigureAwait(false);
        var generatedAtUtc = DateTime.UtcNow;

        var topology = OperationalTopologyAggregation.ComposeOperationalTopology(
            context.Recovery,
            context.CausalitySummary,
            context.Propagation,
            context.Chains,
            context.SituationRoom,
            context.SimulationSummary,
            context.Playbooks,
            context.PatternSummary,
            context.EvolutionTimeline,
            context.IntegrityReport,
            context.ExperienceGraph,
            context.Digest,
            _topologySnapshotStore.GetSnapshots(),
            generatedAtUtc);

        var chains = OperationalTopologyAggregation.ComposeDependencyChains(
            NormalizeArea(context.CausalitySummary.DominantOperationalArea),
            context.Recovery,
            context.CausalitySummary,
            context.Propagation,
            context.Chains,
            context.SituationRoom,
            context.SimulationSummary,
            context.Playbooks,
            context.EvolutionTimeline,
            context.PatternSummary);

        var summary = OperationalTopologyAggregation.ComposeTopologySummary(
            topology,
            context.SituationRoom,
            context.Recovery,
            chains,
            generatedAtUtc);

        _logger.LogInformation(
            "Operational topology observability: summary composed. CriticalityState={CriticalityState}",
            summary.OperationalCriticalityState);

        return summary;
    }

    public async Task<IReadOnlyList<OperationalDependencyChainDto>> GetDependencyChainsAsync(
        CancellationToken cancellationToken = default)
    {
        var context = await LoadTopologyContextAsync(cancellationToken).ConfigureAwait(false);

        var chains = OperationalTopologyAggregation.ComposeDependencyChains(
            NormalizeArea(context.CausalitySummary.DominantOperationalArea),
            context.Recovery,
            context.CausalitySummary,
            context.Propagation,
            context.Chains,
            context.SituationRoom,
            context.SimulationSummary,
            context.Playbooks,
            context.EvolutionTimeline,
            context.PatternSummary);

        _logger.LogInformation(
            "Operational topology observability: dependency chains composed. ChainCount={ChainCount}",
            chains.Count);

        return chains;
    }

    private async Task<OperationalTopologyContext> LoadTopologyContextAsync(CancellationToken cancellationToken)
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
        var chainsResponse = OperationalCausalityAggregation.ComposeChains(
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
            chainsResponse,
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
            chainsResponse,
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
            chainsResponse,
            situationRoom,
            priorSimulationSnapshots,
            generatedAtUtc);

        var simulationSummary = OperationalSimulationAggregation.ComposeSummary(
            simulation,
            situationRoom,
            recovery,
            propagation,
            generatedAtUtc);

        var simulationOutlook = OperationalSimulationAggregation.ComposeOutlook(
            simulation,
            situationRoom,
            recovery,
            propagation);

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

        var priorPatternSnapshots = _patternSnapshotStore.GetSnapshots();
        var patterns = OperationalPatternAggregation.ComposePatterns(
            trend,
            recovery,
            incidentSummary,
            causalitySummary,
            propagation,
            chainsResponse,
            situationRoom,
            simulation,
            playbooks,
            priorPatternSnapshots,
            generatedAtUtc);

        var archetypes = OperationalPatternAggregation.ComposeArchetypesResponse(
            trend,
            recovery,
            incidentSummary,
            causalitySummary,
            propagation,
            situationRoom,
            simulation,
            playbooks,
            priorPatternSnapshots,
            generatedAtUtc);

        var patternSummary = OperationalPatternAggregation.ComposeSummary(
            patterns,
            archetypes,
            situationRoom,
            generatedAtUtc);

        var integrityReport = OperationalIntegrityAggregation.ComposeIntegrityReport(
            trend,
            recovery,
            incidentSummary,
            causalitySummary,
            propagation,
            situationRoom,
            simulation,
            simulationSummary,
            simulationOutlook,
            playbooks,
            patterns,
            patternSummary,
            _integritySnapshotStore.GetSnapshots(),
            generatedAtUtc);

        var experienceGraph = OperationalExperienceGraphAggregation.ComposeExperienceGraph(
            trend,
            timeline,
            triage,
            recovery,
            incidentSummary,
            causalitySummary,
            propagation,
            situationRoom,
            simulationSummary,
            playbooks,
            patternSummary,
            integrityReport,
            _experienceSnapshotStore.GetSnapshots(),
            generatedAtUtc);

        var traversalPaths = OperationalExperienceGraphAggregation.ComposeTraversalPathsResponse(
            trend,
            timeline,
            triage,
            recovery,
            incidentSummary,
            causalitySummary,
            propagation,
            situationRoom,
            simulationSummary,
            playbooks,
            patternSummary,
            integrityReport,
            generatedAtUtc);

        var contextualNavigation = OperationalExperienceGraphAggregation.ComposeContextualNavigation(
            trend,
            timeline,
            triage,
            recovery,
            incidentSummary,
            causalitySummary,
            situationRoom,
            simulationSummary,
            playbooks,
            patternSummary,
            integrityReport,
            generatedAtUtc);

        var digest = OperationalDigestAggregation.ComposeOperationalDigest(
            trend,
            triage,
            recovery,
            incidentSummary,
            causalitySummary,
            propagation,
            situationRoom,
            simulationSummary,
            playbooks,
            patternSummary,
            integrityReport,
            experienceGraph,
            contextualNavigation,
            traversalPaths,
            _digestSnapshotStore.GetSnapshots(),
            generatedAtUtc);

        var evolutionTimeline = OperationalEvolutionAggregation.ComposeEvolutionTimeline(
            trend,
            recovery,
            incidentSummary,
            causalitySummary,
            situationRoom,
            digest,
            integrityReport,
            patternSummary,
            simulationSummary,
            experienceGraph,
            _digestSnapshotStore.GetSnapshots(),
            _integritySnapshotStore.GetSnapshots(),
            _situationSnapshotStore.GetSnapshots(),
            _patternSnapshotStore.GetSnapshots(),
            _experienceSnapshotStore.GetSnapshots(),
            _simulationSnapshotStore.GetSnapshots(),
            _evolutionSnapshotStore.GetSnapshots(),
            generatedAtUtc);

        return new OperationalTopologyContext
        {
            Recovery = recovery,
            CausalitySummary = causalitySummary,
            Propagation = propagation,
            Chains = chainsResponse.Chains,
            SituationRoom = situationRoom,
            SimulationSummary = simulationSummary,
            Playbooks = playbooks,
            PatternSummary = patternSummary,
            IntegrityReport = integrityReport,
            ExperienceGraph = experienceGraph,
            Digest = digest,
            EvolutionTimeline = evolutionTimeline
        };
    }

    private static string NormalizeArea(string area)
    {
        return string.IsNullOrWhiteSpace(area)
            ? OperationalTopologyAggregation.AreaOperational
            : area.Trim();
    }

    private sealed class OperationalTopologyContext
    {
        public OperationalRecoveryPostureDto Recovery { get; init; } = new();
        public OperationalCausalitySummaryDto CausalitySummary { get; init; } = new();
        public OperationalPropagationAnalysisDto Propagation { get; init; } = new();
        public IReadOnlyList<OperationalCausalChainDto> Chains { get; init; } = Array.Empty<OperationalCausalChainDto>();
        public OperationalSituationRoomDto SituationRoom { get; init; } = new();
        public OperationalSimulationSummaryDto SimulationSummary { get; init; } = new();
        public OperationalPlaybooksDto Playbooks { get; init; } = new();
        public OperationalPatternSummaryDto PatternSummary { get; init; } = new();
        public OperationalIntegrityReportDto IntegrityReport { get; init; } = new();
        public OperationalExperienceGraphDto ExperienceGraph { get; init; } = new();
        public OperationalDigestDto Digest { get; init; } = new();
        public OperationalEvolutionTimelineDto EvolutionTimeline { get; init; } = new();
    }
}
