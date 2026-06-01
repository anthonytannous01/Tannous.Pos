using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.OperationalComposition;
using Tannous.Pos.Application.OperationalDashboard;
using Tannous.Pos.Application.OperationalInventoryWorkbench;
using Tannous.Pos.Application.OperationalNavigation;
using Tannous.Pos.Application.OperationalReplayWorkbench;
using Tannous.Pos.Application.OperationalTrends;
using Tannous.Pos.Application.OperationalWorkbench;

namespace Tannous.Pos.Infrastructure.Services;

/// <summary>
/// Operator navigation index via shared composition hub (read-only; no nested workbench service recursion).
/// </summary>
public sealed class OperationalNavigationService : IOperationalNavigationService
{
    private readonly IOperationalReadCompositionHub _compositionHub;
    private readonly IOperationalTrendService _trendService;
    private readonly ILogger<OperationalNavigationService> _logger;

    public OperationalNavigationService(
        IOperationalReadCompositionHub compositionHub,
        IOperationalTrendService trendService,
        ILogger<OperationalNavigationService> logger)
    {
        _compositionHub = compositionHub;
        _trendService = trendService;
        _logger = logger;
    }

    public async Task<OperationalNavigationIndexDto> GetNavigationIndexAsync(CancellationToken cancellationToken = default)
    {
        var context = await LoadNavigationContextAsync(cancellationToken).ConfigureAwait(false);
        var index = OperationalNavigationAggregation.ComposeIndex(
            context.Dashboard,
            context.ReconciliationWorkbench,
            context.InventoryWorkbench,
            context.ReplayPressure,
            context.ReplayStabilization,
            context.TrendSummary,
            context.ReadinessSignals,
            context.RuntimeProtection);

        _logger.LogInformation(
            "Operational navigation observability: index composed. OverallSeverity={OverallSeverity}, Recommendations={Recommendations}, AttentionItems={AttentionItems}",
            index.OverallSeverity,
            index.Recommendations.Count,
            index.AttentionItems.Count);

        return index;
    }

    public async Task<IReadOnlyList<OperationalNavigationRouteDto>> GetNavigationRoutesAsync(
        CancellationToken cancellationToken = default)
    {
        var context = await LoadNavigationContextAsync(cancellationToken).ConfigureAwait(false);
        var routes = OperationalNavigationAggregation.ComposeRoutes(
            context.Dashboard,
            context.ReconciliationWorkbench,
            context.InventoryWorkbench,
            context.ReplayPressure,
            context.ReplayStabilization,
            context.TrendSummary,
            context.ReadinessSignals);

        _logger.LogInformation(
            "Operational navigation observability: routes composed. RouteCount={RouteCount}",
            routes.Count);

        return routes;
    }

    private async Task<OperationalNavigationContext> LoadNavigationContextAsync(CancellationToken cancellationToken)
    {
        await _compositionHub.BuildSnapshotAsync(cancellationToken).ConfigureAwait(false);

        var dashboard = await _compositionHub.GetDashboardSummaryAsync(cancellationToken).ConfigureAwait(false);
        var reconciliationWorkbench = await _compositionHub
            .GetReconciliationWorkbenchViewAsync(cancellationToken)
            .ConfigureAwait(false);
        var inventoryWorkbench = await _compositionHub.GetInventoryWorkbenchViewAsync(cancellationToken).ConfigureAwait(false);
        var runtimeProtection = await _compositionHub.GetRuntimeProtectionSnapshotAsync(cancellationToken).ConfigureAwait(false);
        var governanceOverview = await _compositionHub.GetGovernanceOverviewAsync(cancellationToken).ConfigureAwait(false);
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

        var readinessSignals = new OperationalNavigationReadinessSignals
        {
            ReadinessState = governanceOverview.ReadinessState.ToString(),
            PressureSeverity = governanceOverview.PressureSeverity.ToString(),
            StabilityClassification = governanceOverview.StabilityClassification,
            RuntimeProtectionActive = runtimeProtection.FailsafeActive
                || dashboard.Pressure.ProtectiveModeActive
                || replayPressure.ProtectiveModeVisible
        };

        return new OperationalNavigationContext
        {
            Dashboard = dashboard,
            ReconciliationWorkbench = reconciliationWorkbench,
            InventoryWorkbench = inventoryWorkbench,
            ReplayPressure = replayPressure,
            ReplayStabilization = replayStabilization,
            TrendSummary = trendSummary,
            ReadinessSignals = readinessSignals,
            RuntimeProtection = runtimeProtection
        };
    }

    private sealed class OperationalNavigationContext
    {
        public OperationalDashboardSummaryDto Dashboard { get; init; } = new();
        public OperationalReconciliationWorkbenchDto ReconciliationWorkbench { get; init; } = new();
        public OperationalInventoryWorkbenchDto InventoryWorkbench { get; init; } = new();
        public OperationalReplayPressureSummaryDto ReplayPressure { get; init; } = new();
        public OperationalReplayStabilizationDto ReplayStabilization { get; init; } = new();
        public OperationalTrendSummaryDto TrendSummary { get; init; } = new();
        public OperationalNavigationReadinessSignals ReadinessSignals { get; init; } = new();
        public OperationalGovernanceRuntimeProtectionSnapshot RuntimeProtection { get; init; } = new();
    }
}
