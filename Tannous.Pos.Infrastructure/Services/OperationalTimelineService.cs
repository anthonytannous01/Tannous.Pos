using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.OperationalComposition;
using Tannous.Pos.Application.OperationalDashboard;
using Tannous.Pos.Application.OperationalInventoryWorkbench;
using Tannous.Pos.Application.OperationalReplayWorkbench;
using Tannous.Pos.Application.OperationalTimeline;
using Tannous.Pos.Application.OperationalTrends;
using Tannous.Pos.Application.OperationalWorkbench;

namespace Tannous.Pos.Infrastructure.Services;

/// <summary>
/// Operator operational timeline via shared composition hub (request-time only; no nested workbench service recursion).
/// </summary>
public sealed class OperationalTimelineService : IOperationalTimelineService
{
    private readonly IOperationalReadCompositionHub _compositionHub;
    private readonly IOperationalTrendService _trendService;
    private readonly IOperationalTimelineWindowStore _windowStore;
    private readonly ILogger<OperationalTimelineService> _logger;

    public OperationalTimelineService(
        IOperationalReadCompositionHub compositionHub,
        IOperationalTrendService trendService,
        IOperationalTimelineWindowStore windowStore,
        ILogger<OperationalTimelineService> logger)
    {
        _compositionHub = compositionHub;
        _trendService = trendService;
        _windowStore = windowStore;
        _logger = logger;
    }

    public async Task<OperationalTimelineDto> GetTimelineAsync(CancellationToken cancellationToken = default)
    {
        var capture = await RefreshTimelineAsync(cancellationToken).ConfigureAwait(false);
        var timeline = OperationalTimelineAggregation.ComposeTimeline(_windowStore.GetEvents());

        _logger.LogInformation(
            "Operational timeline observability: timeline composed. EventCount={EventCount}, AttentionItems={AttentionItems}, CaptureProtective={CaptureProtective}",
            timeline.EventCount,
            timeline.AttentionItems.Count,
            capture.ProtectiveModeActive);

        return timeline;
    }

    public async Task<IReadOnlyList<OperationalTimelineCorrelationDto>> GetCorrelationsAsync(
        CancellationToken cancellationToken = default)
    {
        await RefreshTimelineAsync(cancellationToken).ConfigureAwait(false);
        var correlations = OperationalTimelineAggregation.ComposeCorrelations(_windowStore.GetEvents());

        _logger.LogInformation(
            "Operational timeline observability: correlations composed. CorrelationCount={CorrelationCount}",
            correlations.Count);

        return correlations;
    }

    private async Task<OperationalTimelineCaptureSnapshot> RefreshTimelineAsync(CancellationToken cancellationToken)
    {
        var priorCapture = _windowStore.GetLastCapture();
        var context = await LoadOperationalContextAsync(cancellationToken).ConfigureAwait(false);

        var capture = OperationalTimelineAggregation.BuildCaptureSnapshot(
            context.Dashboard,
            context.ReconciliationWorkbench,
            context.InventoryWorkbench,
            context.ReplayPressure,
            context.ReplayStabilization,
            context.TrendSummary,
            context.Fingerprint,
            context.RuntimeProtection);

        var transitionEvents = OperationalTimelineAggregation.DetectTransitionEvents(capture, priorCapture);
        foreach (var timelineEvent in transitionEvents)
            _windowStore.Append(timelineEvent);

        _windowStore.SetLastCapture(capture);
        return capture;
    }

    private async Task<OperationalTimelineContext> LoadOperationalContextAsync(CancellationToken cancellationToken)
    {
        await _compositionHub.BuildSnapshotAsync(cancellationToken).ConfigureAwait(false);

        var dashboard = await _compositionHub.GetDashboardSummaryAsync(cancellationToken).ConfigureAwait(false);
        var reconciliationWorkbench = await _compositionHub
            .GetReconciliationWorkbenchViewAsync(cancellationToken)
            .ConfigureAwait(false);
        var inventoryWorkbench = await _compositionHub.GetInventoryWorkbenchViewAsync(cancellationToken).ConfigureAwait(false);
        var runtimeProtection = await _compositionHub.GetRuntimeProtectionSnapshotAsync(cancellationToken).ConfigureAwait(false);
        var fingerprint = await _compositionHub.GetFingerprintSnapshotAsync(cancellationToken).ConfigureAwait(false);
        var trendSummary = await _trendService.GetSummaryAsync(cancellationToken).ConfigureAwait(false);

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

        return new OperationalTimelineContext
        {
            Dashboard = dashboard,
            ReconciliationWorkbench = reconciliationWorkbench,
            InventoryWorkbench = inventoryWorkbench,
            ReplayPressure = replayPressure,
            ReplayStabilization = replayStabilization,
            TrendSummary = trendSummary,
            Fingerprint = fingerprint,
            RuntimeProtection = runtimeProtection
        };
    }

    private sealed class OperationalTimelineContext
    {
        public OperationalDashboardSummaryDto Dashboard { get; init; } = new();
        public OperationalReconciliationWorkbenchDto ReconciliationWorkbench { get; init; } = new();
        public OperationalInventoryWorkbenchDto InventoryWorkbench { get; init; } = new();
        public OperationalReplayPressureSummaryDto ReplayPressure { get; init; } = new();
        public OperationalReplayStabilizationDto ReplayStabilization { get; init; } = new();
        public OperationalTrendSummaryDto TrendSummary { get; init; } = new();
        public OperationalGovernanceFingerprintSnapshot Fingerprint { get; init; } = new();
        public OperationalGovernanceRuntimeProtectionSnapshot RuntimeProtection { get; init; } = new();
    }
}
