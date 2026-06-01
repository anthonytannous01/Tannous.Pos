using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.OperationalComposition;
using Tannous.Pos.Application.OperationalDashboard;
using Tannous.Pos.Application.OperationalInventoryWorkbench;
using Tannous.Pos.Application.OperationalNavigation;
using Tannous.Pos.Application.OperationalReplayWorkbench;
using Tannous.Pos.Application.OperationalTimeline;
using Tannous.Pos.Application.OperationalTrends;
using Tannous.Pos.Application.OperationalTriage;
using Tannous.Pos.Application.OperationalWorkbench;

namespace Tannous.Pos.Infrastructure.Services;

/// <summary>
/// Operator triage queue via composition hub and existing operational read services (no workbench service recursion).
/// </summary>
public sealed class OperationalTriageService : IOperationalTriageService
{
    private readonly IOperationalReadCompositionHub _compositionHub;
    private readonly IOperationalTrendService _trendService;
    private readonly IOperationalTimelineService _timelineService;
    private readonly IOperationalNavigationService _navigationService;
    private readonly ILogger<OperationalTriageService> _logger;

    public OperationalTriageService(
        IOperationalReadCompositionHub compositionHub,
        IOperationalTrendService trendService,
        IOperationalTimelineService timelineService,
        IOperationalNavigationService navigationService,
        ILogger<OperationalTriageService> logger)
    {
        _compositionHub = compositionHub;
        _trendService = trendService;
        _timelineService = timelineService;
        _navigationService = navigationService;
        _logger = logger;
    }

    public async Task<OperationalTriageQueueDto> GetTriageQueueAsync(CancellationToken cancellationToken = default)
    {
        var context = await LoadTriageContextAsync(cancellationToken).ConfigureAwait(false);
        var queue = OperationalTriageAggregation.ComposeQueue(
            context.Navigation,
            context.Timeline,
            context.TimelineCorrelations,
            context.Trend,
            context.Dashboard,
            context.ReplayPressure,
            context.ReplayStabilization,
            context.ReconciliationWorkbench,
            context.InventoryWorkbench,
            context.RuntimeSaturationIndicated);

        _logger.LogInformation(
            "Operational triage observability: queue composed. ItemCount={ItemCount}, OverallPriority={OverallPriority}, Correlations={Correlations}",
            queue.ItemCount,
            queue.OverallPriority,
            queue.Correlations.Count);

        return queue;
    }

    public async Task<IReadOnlyList<OperationalTriageRecommendationDto>> GetRecommendationsAsync(
        CancellationToken cancellationToken = default)
    {
        var context = await LoadTriageContextAsync(cancellationToken).ConfigureAwait(false);
        var recommendations = OperationalTriageAggregation.ComposeRecommendations(
            context.Navigation,
            context.Timeline,
            context.Trend,
            context.ReplayPressure,
            context.ReplayStabilization,
            context.ReconciliationWorkbench,
            context.InventoryWorkbench,
            context.RuntimeSaturationIndicated,
            context.ProtectiveModeActive);

        _logger.LogInformation(
            "Operational triage observability: recommendations composed. RecommendationCount={RecommendationCount}",
            recommendations.Count);

        return recommendations;
    }

    private async Task<OperationalTriageContext> LoadTriageContextAsync(CancellationToken cancellationToken)
    {
        var trend = await _trendService.GetSummaryAsync(cancellationToken).ConfigureAwait(false);
        var navigation = await _navigationService.GetNavigationIndexAsync(cancellationToken).ConfigureAwait(false);
        var timeline = await _timelineService.GetTimelineAsync(cancellationToken).ConfigureAwait(false);
        var timelineCorrelations = await _timelineService.GetCorrelationsAsync(cancellationToken).ConfigureAwait(false);

        await _compositionHub.BuildSnapshotAsync(cancellationToken).ConfigureAwait(false);

        var dashboard = await _compositionHub.GetDashboardSummaryAsync(cancellationToken).ConfigureAwait(false);
        var reconciliationWorkbench = await _compositionHub
            .GetReconciliationWorkbenchViewAsync(cancellationToken)
            .ConfigureAwait(false);
        var inventoryWorkbench = await _compositionHub.GetInventoryWorkbenchViewAsync(cancellationToken).ConfigureAwait(false);
        var runtimeProtection = await _compositionHub.GetRuntimeProtectionSnapshotAsync(cancellationToken).ConfigureAwait(false);

        var resilience = await _compositionHub.GetResilienceSummaryAsync(cancellationToken).ConfigureAwait(false);
        var reconciliation = await _compositionHub.GetReconciliationSummaryAsync(cancellationToken).ConfigureAwait(false);
        var alerts = await _compositionHub.GetAlertSummaryAsync(cancellationToken).ConfigureAwait(false);
        var incidents = await _compositionHub.GetIncidentsSummaryAsync(cancellationToken).ConfigureAwait(false);

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

        var protectiveModeActive = dashboard.Pressure.ProtectiveModeActive
            || replayPressure.ProtectiveModeVisible
            || runtimeProtection.FailsafeActive;

        return new OperationalTriageContext
        {
            Trend = trend,
            Navigation = navigation,
            Timeline = timeline,
            TimelineCorrelations = timelineCorrelations,
            Dashboard = dashboard,
            ReconciliationWorkbench = reconciliationWorkbench,
            InventoryWorkbench = inventoryWorkbench,
            ReplayPressure = replayPressure,
            ReplayStabilization = replayStabilization,
            RuntimeSaturationIndicated = runtimeSignals.RuntimeSaturationIndicated,
            ProtectiveModeActive = protectiveModeActive
        };
    }

    private sealed class OperationalTriageContext
    {
        public OperationalTrendSummaryDto Trend { get; init; } = new();
        public OperationalNavigationIndexDto Navigation { get; init; } = new();
        public OperationalTimelineDto Timeline { get; init; } = new();
        public IReadOnlyList<OperationalTimelineCorrelationDto> TimelineCorrelations { get; init; } =
            Array.Empty<OperationalTimelineCorrelationDto>();
        public OperationalDashboardSummaryDto Dashboard { get; init; } = new();
        public OperationalReconciliationWorkbenchDto ReconciliationWorkbench { get; init; } = new();
        public OperationalInventoryWorkbenchDto InventoryWorkbench { get; init; } = new();
        public OperationalReplayPressureSummaryDto ReplayPressure { get; init; } = new();
        public OperationalReplayStabilizationDto ReplayStabilization { get; init; } = new();
        public bool RuntimeSaturationIndicated { get; init; }
        public bool ProtectiveModeActive { get; init; }
    }
}
