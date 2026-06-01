using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.OperationalCausality;
using Tannous.Pos.Application.OperationalComposition;
using Tannous.Pos.Application.OperationalDashboard;
using Tannous.Pos.Application.OperationalIncidents;
using Tannous.Pos.Application.OperationalInventoryWorkbench;
using Tannous.Pos.Application.OperationalRecovery;
using Tannous.Pos.Application.OperationalReplayWorkbench;
using Tannous.Pos.Application.OperationalSituationRoom;
using Tannous.Pos.Application.OperationalTimeline;
using Tannous.Pos.Application.OperationalTrends;
using Tannous.Pos.Application.OperationalTriage;
using Tannous.Pos.Application.OperationalWorkbench;

namespace Tannous.Pos.Infrastructure.Services;

/// <summary>
/// Operator situation room briefing via composition hub and existing operational read services (no workbench/causality service recursion).
/// </summary>
public sealed class OperationalSituationRoomService : IOperationalSituationRoomService
{
    private readonly IOperationalReadCompositionHub _compositionHub;
    private readonly IOperationalTrendService _trendService;
    private readonly IOperationalTimelineService _timelineService;
    private readonly IOperationalTriageService _triageService;
    private readonly IOperationalRecoveryService _recoveryService;
    private readonly IOperationalIncidentCaseStore _incidentCaseStore;
    private readonly IOperationalCausalitySnapshotStore _causalitySnapshotStore;
    private readonly IOperationalSituationSnapshotStore _situationSnapshotStore;
    private readonly ILogger<OperationalSituationRoomService> _logger;

    public OperationalSituationRoomService(
        IOperationalReadCompositionHub compositionHub,
        IOperationalTrendService trendService,
        IOperationalTimelineService timelineService,
        IOperationalTriageService triageService,
        IOperationalRecoveryService recoveryService,
        IOperationalIncidentCaseStore incidentCaseStore,
        IOperationalCausalitySnapshotStore causalitySnapshotStore,
        IOperationalSituationSnapshotStore situationSnapshotStore,
        ILogger<OperationalSituationRoomService> logger)
    {
        _compositionHub = compositionHub;
        _trendService = trendService;
        _timelineService = timelineService;
        _triageService = triageService;
        _recoveryService = recoveryService;
        _incidentCaseStore = incidentCaseStore;
        _causalitySnapshotStore = causalitySnapshotStore;
        _situationSnapshotStore = situationSnapshotStore;
        _logger = logger;
    }

    public async Task<OperationalSituationRoomDto> GetSituationRoomAsync(CancellationToken cancellationToken = default)
    {
        var context = await LoadSituationContextAsync(cancellationToken).ConfigureAwait(false);
        var priorSnapshots = _situationSnapshotStore.GetSnapshots();
        var generatedAtUtc = DateTime.UtcNow;

        var room = OperationalSituationRoomAggregation.ComposeSituationRoom(
            context.Dashboard,
            context.Trend,
            context.Triage,
            context.Recovery,
            context.RecoveryOutlook,
            context.IncidentSummary,
            context.CausalitySummary,
            context.Propagation,
            context.Chains,
            priorSnapshots,
            generatedAtUtc);

        _situationSnapshotStore.Append(OperationalSituationRoomAggregation.CreateSnapshot(room));

        _logger.LogInformation(
            "Operational situation room observability: briefing composed. AttentionLevel={AttentionLevel}, NarrativeCount={NarrativeCount}",
            room.AttentionLevel,
            room.Narratives.Count);

        return room;
    }

    public async Task<OperationalExecutiveBriefingDto> GetExecutiveBriefingAsync(CancellationToken cancellationToken = default)
    {
        var context = await LoadSituationContextAsync(cancellationToken).ConfigureAwait(false);
        var generatedAtUtc = DateTime.UtcNow;

        var room = OperationalSituationRoomAggregation.ComposeSituationRoom(
            context.Dashboard,
            context.Trend,
            context.Triage,
            context.Recovery,
            context.RecoveryOutlook,
            context.IncidentSummary,
            context.CausalitySummary,
            context.Propagation,
            context.Chains,
            _situationSnapshotStore.GetSnapshots(),
            generatedAtUtc);

        var briefing = OperationalSituationRoomAggregation.ComposeExecutiveBriefing(room);

        _logger.LogInformation(
            "Operational situation room observability: executive briefing composed. Confidence={Confidence}",
            briefing.Confidence);

        return briefing;
    }

    public async Task<OperationalSituationSummaryDto> GetSituationSummaryAsync(CancellationToken cancellationToken = default)
    {
        var context = await LoadSituationContextAsync(cancellationToken).ConfigureAwait(false);
        var generatedAtUtc = DateTime.UtcNow;

        var room = OperationalSituationRoomAggregation.ComposeSituationRoom(
            context.Dashboard,
            context.Trend,
            context.Triage,
            context.Recovery,
            context.RecoveryOutlook,
            context.IncidentSummary,
            context.CausalitySummary,
            context.Propagation,
            context.Chains,
            _situationSnapshotStore.GetSnapshots(),
            generatedAtUtc);

        var summary = OperationalSituationRoomAggregation.ComposeSituationSummary(room, context.CausalitySummary);

        _logger.LogInformation(
            "Operational situation room observability: summary composed. PlatformState={PlatformState}, ExecutiveAttention={ExecutiveAttention}",
            summary.PlatformState,
            summary.ExecutiveAttentionRequired);

        return summary;
    }

    private async Task<OperationalSituationContext> LoadSituationContextAsync(CancellationToken cancellationToken)
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

        return new OperationalSituationContext
        {
            Dashboard = dashboard,
            Trend = trend,
            Triage = triage,
            Recovery = recovery,
            RecoveryOutlook = recoveryOutlook,
            IncidentSummary = incidentSummary,
            CausalitySummary = causalitySummary,
            Propagation = propagation,
            Chains = chains
        };
    }

    private sealed class OperationalSituationContext
    {
        public OperationalDashboardSummaryDto Dashboard { get; init; } = new();
        public OperationalTrendSummaryDto Trend { get; init; } = new();
        public OperationalTriageQueueDto Triage { get; init; } = new();
        public OperationalRecoveryPostureDto Recovery { get; init; } = new();
        public OperationalRecoveryOutlookDto RecoveryOutlook { get; init; } = new();
        public OperationalIncidentCasesSummaryDto IncidentSummary { get; init; } = new();
        public OperationalCausalitySummaryDto CausalitySummary { get; init; } = new();
        public OperationalPropagationAnalysisDto Propagation { get; init; } = new();
        public OperationalCausalChainsDto Chains { get; init; } = new();
    }
}
