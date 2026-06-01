using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.OperationalAttention;
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
using Tannous.Pos.Application.OperationalResilience;
using Tannous.Pos.Application.OperationalConvergence;
using Tannous.Pos.Application.OperationalTimeline;
using Tannous.Pos.Application.OperationalTopology;
using Tannous.Pos.Application.OperationalTrends;
using Tannous.Pos.Application.OperationalTriage;
using Tannous.Pos.Application.OperationalWorkbench;

namespace Tannous.Pos.Infrastructure.Services;

/// <summary>
/// Operator attention coordination via composition hub and existing operational read services (no attention/resilience service recursion).
/// </summary>
public sealed class OperationalAttentionService : IOperationalAttentionService
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
    private readonly IOperationalConvergenceSnapshotStore _convergenceSnapshotStore;
    private readonly IOperationalAttentionSnapshotStore _attentionSnapshotStore;
    private readonly ILogger<OperationalAttentionService> _logger;

    public OperationalAttentionService(
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
        IOperationalConvergenceSnapshotStore convergenceSnapshotStore,
        IOperationalAttentionSnapshotStore attentionSnapshotStore,
        ILogger<OperationalAttentionService> logger)
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
        _convergenceSnapshotStore = convergenceSnapshotStore;
        _attentionSnapshotStore = attentionSnapshotStore;
        _logger = logger;
    }

    public async Task<OperationalAttentionReportDto> GetAttentionReportAsync(
        CancellationToken cancellationToken = default)
    {
        var context = await LoadAttentionContextAsync(cancellationToken).ConfigureAwait(false);
        var priorSnapshots = _attentionSnapshotStore.GetSnapshots();
        var generatedAtUtc = DateTime.UtcNow;

        var report = OperationalAttentionAggregation.ComposeAttentionReport(
            context.Recovery,
            context.SituationRoom,
            context.CausalitySummary,
            context.Propagation,
            context.SimulationSummary,
            context.Playbooks,
            context.Digest,
            context.EvolutionTimeline,
            context.Topology,
            context.ConvergenceReport,
            context.ResilienceReport,
            context.Fragilities,
            priorSnapshots,
            generatedAtUtc);

        _attentionSnapshotStore.Append(OperationalAttentionAggregation.CreateSnapshot(report, report.Priorities));

        _logger.LogInformation(
            "Operational attention observability: report composed. DominantPriority={DominantPriority}, PriorityCount={PriorityCount}",
            report.DominantOperationalPriority,
            report.Priorities.Count);

        return report;
    }

    public async Task<OperationalAttentionSummaryDto> GetAttentionSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var context = await LoadAttentionContextAsync(cancellationToken).ConfigureAwait(false);
        var generatedAtUtc = DateTime.UtcNow;

        var report = OperationalAttentionAggregation.ComposeAttentionReport(
            context.Recovery,
            context.SituationRoom,
            context.CausalitySummary,
            context.Propagation,
            context.SimulationSummary,
            context.Playbooks,
            context.Digest,
            context.EvolutionTimeline,
            context.Topology,
            context.ConvergenceReport,
            context.ResilienceReport,
            context.Fragilities,
            _attentionSnapshotStore.GetSnapshots(),
            generatedAtUtc);

        var summary = OperationalAttentionAggregation.ComposeAttentionSummary(
            report,
            context.SituationRoom,
            report.Priorities,
            generatedAtUtc);

        _logger.LogInformation(
            "Operational attention observability: summary composed. AttentionState={AttentionState}",
            summary.OperationalAttentionState);

        return summary;
    }

    public async Task<IReadOnlyList<OperationalPriorityDto>> GetOperationalPrioritiesAsync(
        CancellationToken cancellationToken = default)
    {
        var context = await LoadAttentionContextAsync(cancellationToken).ConfigureAwait(false);

        var priorities = OperationalAttentionAggregation.ComposePriorities(
            NormalizeArea(context.CausalitySummary.DominantOperationalArea),
            context.Recovery,
            context.SituationRoom,
            context.CausalitySummary,
            context.Propagation,
            context.SimulationSummary,
            context.Playbooks,
            context.Digest,
            context.EvolutionTimeline,
            context.Topology,
            context.ConvergenceReport,
            context.ResilienceReport,
            context.Fragilities);

        _logger.LogInformation(
            "Operational attention observability: priorities composed. PriorityCount={PriorityCount}",
            priorities.Count);

        return priorities;
    }

    private async Task<OperationalAttentionContext> LoadAttentionContextAsync(CancellationToken cancellationToken)
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

        var topology = OperationalTopologyAggregation.ComposeOperationalTopology(
            recovery,
            causalitySummary,
            propagation,
            chainsResponse.Chains,
            situationRoom,
            simulationSummary,
            playbooks,
            patternSummary,
            evolutionTimeline,
            integrityReport,
            experienceGraph,
            digest,
            _topologySnapshotStore.GetSnapshots(),
            generatedAtUtc);

        var convergenceReport = OperationalConvergenceAggregation.ComposeConvergenceReport(
            recovery,
            causalitySummary,
            propagation,
            situationRoom,
            simulationSummary,
            playbooks,
            patternSummary,
            evolutionTimeline,
            integrityReport,
            experienceGraph,
            digest,
            topology,
            _convergenceSnapshotStore.GetSnapshots(),
            generatedAtUtc);

        var protectiveContainmentActive = protectiveModeActive || runtimeSignals.ProtectiveContainmentActive;

        var resilienceReport = OperationalResilienceAggregation.ComposeResilienceReport(
            recovery,
            situationRoom,
            simulationSummary,
            patternSummary,
            evolutionTimeline,
            integrityReport,
            topology,
            convergenceReport,
            protectiveContainmentActive,
            Array.Empty<OperationalResilienceCognitionSnapshot>(),
            generatedAtUtc);

        var fragilities = OperationalResilienceAggregation.ComposeFragilities(
            NormalizeArea(causalitySummary.DominantOperationalArea),
            recovery,
            situationRoom,
            simulationSummary,
            patternSummary,
            evolutionTimeline,
            integrityReport,
            topology,
            convergenceReport,
            protectiveContainmentActive);

        return new OperationalAttentionContext
        {
            Recovery = recovery,
            CausalitySummary = causalitySummary,
            Propagation = propagation,
            SituationRoom = situationRoom,
            SimulationSummary = simulationSummary,
            Playbooks = playbooks,
            PatternSummary = patternSummary,
            IntegrityReport = integrityReport,
            ExperienceGraph = experienceGraph,
            Digest = digest,
            EvolutionTimeline = evolutionTimeline,
            Topology = topology,
            ConvergenceReport = convergenceReport,
            ProtectiveContainmentActive = protectiveContainmentActive,
            ResilienceReport = resilienceReport,
            Fragilities = fragilities
        };
    }

    private static string NormalizeArea(string area)
    {
        return string.IsNullOrWhiteSpace(area)
            ? OperationalAttentionAggregation.AreaOperational
            : area.Trim();
    }

    private sealed class OperationalAttentionContext
    {
        public OperationalRecoveryPostureDto Recovery { get; init; } = new();
        public OperationalCausalitySummaryDto CausalitySummary { get; init; } = new();
        public OperationalPropagationAnalysisDto Propagation { get; init; } = new();
        public OperationalSituationRoomDto SituationRoom { get; init; } = new();
        public OperationalSimulationSummaryDto SimulationSummary { get; init; } = new();
        public OperationalPlaybooksDto Playbooks { get; init; } = new();
        public OperationalPatternSummaryDto PatternSummary { get; init; } = new();
        public OperationalIntegrityReportDto IntegrityReport { get; init; } = new();
        public OperationalExperienceGraphDto ExperienceGraph { get; init; } = new();
        public OperationalDigestDto Digest { get; init; } = new();
        public OperationalEvolutionTimelineDto EvolutionTimeline { get; init; } = new();
        public OperationalTopologyDto Topology { get; init; } = new();
        public OperationalConvergenceReportDto ConvergenceReport { get; init; } = new();
        public bool ProtectiveContainmentActive { get; init; }
        public OperationalResilienceReportDto ResilienceReport { get; init; } = new();
        public IReadOnlyList<OperationalFragilityDto> Fragilities { get; init; } = Array.Empty<OperationalFragilityDto>();
    }
}
