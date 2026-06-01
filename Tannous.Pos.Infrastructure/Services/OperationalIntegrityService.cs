using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.OperationalCausality;
using Tannous.Pos.Application.OperationalComposition;
using Tannous.Pos.Application.OperationalDashboard;
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
using Tannous.Pos.Application.OperationalTrends;
using Tannous.Pos.Application.OperationalTriage;
using Tannous.Pos.Application.OperationalWorkbench;

namespace Tannous.Pos.Infrastructure.Services;

/// <summary>
/// Operator interpretation integrity via composition hub and existing operational read services (no integrity/pattern service recursion).
/// </summary>
public sealed class OperationalIntegrityService : IOperationalIntegrityService
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
    private readonly ILogger<OperationalIntegrityService> _logger;

    public OperationalIntegrityService(
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
        ILogger<OperationalIntegrityService> logger)
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
        _logger = logger;
    }

    public async Task<OperationalIntegrityReportDto> GetIntegrityReportAsync(CancellationToken cancellationToken = default)
    {
        var context = await LoadIntegrityContextAsync(cancellationToken).ConfigureAwait(false);
        var priorSnapshots = _integritySnapshotStore.GetSnapshots();
        var generatedAtUtc = DateTime.UtcNow;

        var report = OperationalIntegrityAggregation.ComposeIntegrityReport(
            context.Trend,
            context.Recovery,
            context.IncidentSummary,
            context.CausalitySummary,
            context.Propagation,
            context.SituationRoom,
            context.Simulation,
            context.SimulationSummary,
            context.SimulationOutlook,
            context.Playbooks,
            context.Patterns,
            context.PatternSummary,
            priorSnapshots,
            generatedAtUtc);

        _integritySnapshotStore.Append(OperationalIntegrityAggregation.CreateSnapshot(report));

        _logger.LogInformation(
            "Operational integrity observability: report composed. IntegrityState={IntegrityState}, ConsistencyScore={ConsistencyScore}, ContradictionCount={ContradictionCount}",
            report.OverallIntegrityState,
            report.ConsistencyScore,
            report.ContradictionCount);

        return report;
    }

    public async Task<OperationalIntegritySummaryDto> GetIntegritySummaryAsync(CancellationToken cancellationToken = default)
    {
        var context = await LoadIntegrityContextAsync(cancellationToken).ConfigureAwait(false);
        var generatedAtUtc = DateTime.UtcNow;

        var report = OperationalIntegrityAggregation.ComposeIntegrityReport(
            context.Trend,
            context.Recovery,
            context.IncidentSummary,
            context.CausalitySummary,
            context.Propagation,
            context.SituationRoom,
            context.Simulation,
            context.SimulationSummary,
            context.SimulationOutlook,
            context.Playbooks,
            context.Patterns,
            context.PatternSummary,
            _integritySnapshotStore.GetSnapshots(),
            generatedAtUtc);

        var summary = OperationalIntegrityAggregation.ComposeSummary(
            report,
            context.SituationRoom,
            generatedAtUtc);

        _logger.LogInformation(
            "Operational integrity observability: summary composed. IntegrityState={IntegrityState}, AlignmentStrength={AlignmentStrength}",
            summary.IntegrityState,
            summary.AlignmentStrength);

        return summary;
    }

    public async Task<OperationalIntegrityContradictionsDto> GetContradictionsAsync(
        CancellationToken cancellationToken = default)
    {
        var context = await LoadIntegrityContextAsync(cancellationToken).ConfigureAwait(false);
        var generatedAtUtc = DateTime.UtcNow;

        var contradictions = OperationalIntegrityAggregation.ComposeContradictionsResponse(
            context.Trend,
            context.Recovery,
            context.IncidentSummary,
            context.CausalitySummary,
            context.Propagation,
            context.SituationRoom,
            context.Simulation,
            context.SimulationSummary,
            context.SimulationOutlook,
            context.Playbooks,
            generatedAtUtc);

        _logger.LogInformation(
            "Operational integrity observability: contradictions composed. ContradictionCount={ContradictionCount}",
            contradictions.ContradictionCount);

        return contradictions;
    }

    private async Task<OperationalIntegrityContext> LoadIntegrityContextAsync(CancellationToken cancellationToken)
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
            chains,
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

        return new OperationalIntegrityContext
        {
            Trend = trend,
            Recovery = recovery,
            IncidentSummary = incidentSummary,
            CausalitySummary = causalitySummary,
            Propagation = propagation,
            SituationRoom = situationRoom,
            Simulation = simulation,
            SimulationSummary = simulationSummary,
            SimulationOutlook = simulationOutlook,
            Playbooks = playbooks,
            Patterns = patterns,
            PatternSummary = patternSummary
        };
    }

    private sealed class OperationalIntegrityContext
    {
        public OperationalTrendSummaryDto Trend { get; init; } = new();
        public OperationalRecoveryPostureDto Recovery { get; init; } = new();
        public OperationalIncidentCasesSummaryDto IncidentSummary { get; init; } = new();
        public OperationalCausalitySummaryDto CausalitySummary { get; init; } = new();
        public OperationalPropagationAnalysisDto Propagation { get; init; } = new();
        public OperationalSituationRoomDto SituationRoom { get; init; } = new();
        public OperationalSimulationScenariosDto Simulation { get; init; } = new();
        public OperationalSimulationSummaryDto SimulationSummary { get; init; } = new();
        public OperationalSimulationOutlookDto SimulationOutlook { get; init; } = new();
        public OperationalPlaybooksDto Playbooks { get; init; } = new();
        public OperationalPatternsDto Patterns { get; init; } = new();
        public OperationalPatternSummaryDto PatternSummary { get; init; } = new();
    }
}
