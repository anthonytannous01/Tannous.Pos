using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.OperationalComposition;
using Tannous.Pos.Application.OperationalDashboard;
using Tannous.Pos.Application.OperationalInventoryWorkbench;
using Tannous.Pos.Application.OperationalReplayWorkbench;
using Tannous.Pos.Application.OperationalTimeline;
using Tannous.Pos.Application.OperationalTrends;
using Tannous.Pos.Application.OperationalTriage;
using Tannous.Pos.Application.OperationalRecovery;
using Tannous.Pos.Application.OperationalWorkbench;

namespace Tannous.Pos.Infrastructure.Services;

/// <summary>
/// Operator recovery posture via composition hub and existing operational read services (no workbench service recursion).
/// </summary>
public sealed class OperationalRecoveryService : IOperationalRecoveryService
{
    private readonly IOperationalReadCompositionHub _compositionHub;
    private readonly IOperationalTrendService _trendService;
    private readonly IOperationalTimelineService _timelineService;
    private readonly IOperationalTriageService _triageService;
    private readonly ILogger<OperationalRecoveryService> _logger;

    public OperationalRecoveryService(
        IOperationalReadCompositionHub compositionHub,
        IOperationalTrendService trendService,
        IOperationalTimelineService timelineService,
        IOperationalTriageService triageService,
        ILogger<OperationalRecoveryService> logger)
    {
        _compositionHub = compositionHub;
        _trendService = trendService;
        _timelineService = timelineService;
        _triageService = triageService;
        _logger = logger;
    }

    public async Task<OperationalRecoveryPostureDto> GetRecoveryPostureAsync(CancellationToken cancellationToken = default)
    {
        var context = await LoadRecoveryContextAsync(cancellationToken).ConfigureAwait(false);
        var posture = OperationalRecoveryAggregation.ComposePosture(
            context.Trend,
            context.Timeline,
            context.TimelineCorrelations,
            context.Triage,
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

        _logger.LogInformation(
            "Operational recovery observability: posture composed. State={State}, Direction={Direction}, SignalCount={SignalCount}",
            posture.OverallState,
            posture.OverallDirection,
            posture.SignalCount);

        return posture;
    }

    public async Task<OperationalRecoveryOutlookDto> GetRecoveryOutlookAsync(CancellationToken cancellationToken = default)
    {
        var context = await LoadRecoveryContextAsync(cancellationToken).ConfigureAwait(false);
        var outlook = OperationalRecoveryAggregation.ComposeOutlook(
            context.Trend,
            context.Timeline,
            context.Triage,
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

        _logger.LogInformation(
            "Operational recovery observability: outlook composed. State={State}, SectionCount={SectionCount}, ConvergenceCount={ConvergenceCount}",
            outlook.OverallState,
            outlook.SectionCount,
            outlook.ConvergenceCount);

        return outlook;
    }

    private async Task<OperationalRecoveryContext> LoadRecoveryContextAsync(CancellationToken cancellationToken)
    {
        var trend = await _trendService.GetSummaryAsync(cancellationToken).ConfigureAwait(false);
        var timeline = await _timelineService.GetTimelineAsync(cancellationToken).ConfigureAwait(false);
        var timelineCorrelations = await _timelineService.GetCorrelationsAsync(cancellationToken).ConfigureAwait(false);
        var triage = await _triageService.GetTriageQueueAsync(cancellationToken).ConfigureAwait(false);

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

        return new OperationalRecoveryContext
        {
            Trend = trend,
            Timeline = timeline,
            TimelineCorrelations = timelineCorrelations,
            Triage = triage,
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

    private sealed class OperationalRecoveryContext
    {
        public OperationalTrendSummaryDto Trend { get; init; } = new();
        public OperationalTimelineDto Timeline { get; init; } = new();
        public IReadOnlyList<OperationalTimelineCorrelationDto> TimelineCorrelations { get; init; } =
            Array.Empty<OperationalTimelineCorrelationDto>();
        public OperationalTriageQueueDto Triage { get; init; } = new();
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
