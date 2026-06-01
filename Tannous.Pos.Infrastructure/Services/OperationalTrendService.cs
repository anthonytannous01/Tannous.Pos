using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.OperationalComposition;
using Tannous.Pos.Application.OperationalReplayWorkbench;
using Tannous.Pos.Application.OperationalTrends;

namespace Tannous.Pos.Infrastructure.Services;

/// <summary>
/// Operator short-window trend read model via shared composition hub (request-time only; no nested workbench recursion).
/// </summary>
public sealed class OperationalTrendService : IOperationalTrendService
{
    private readonly IOperationalReadCompositionHub _compositionHub;
    private readonly IOperationalTrendWindowStore _windowStore;
    private readonly ILogger<OperationalTrendService> _logger;

    public OperationalTrendService(
        IOperationalReadCompositionHub compositionHub,
        IOperationalTrendWindowStore windowStore,
        ILogger<OperationalTrendService> logger)
    {
        _compositionHub = compositionHub;
        _windowStore = windowStore;
        _logger = logger;
    }

    public async Task<OperationalTrendSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var (current, priorSnapshots) = await BuildCurrentSnapshotAsync(cancellationToken).ConfigureAwait(false);
        var summary = OperationalTrendAggregation.ComposeSummary(current, priorSnapshots);
        _windowStore.Append(current);

        _logger.LogInformation(
            "Operational trend observability: summary composed. Direction={Direction}, Severity={Severity}, AttentionItems={AttentionItems}, SnapshotCount={SnapshotCount}",
            summary.OverallDirection,
            summary.Severity,
            summary.AttentionItems.Count,
            summary.Window.SnapshotCount);

        return summary;
    }

    public async Task<IReadOnlyList<OperationalTrendDeltaDto>> GetDeltasAsync(CancellationToken cancellationToken = default)
    {
        var (current, priorSnapshots) = await BuildCurrentSnapshotAsync(cancellationToken).ConfigureAwait(false);
        var deltas = OperationalTrendAggregation.ComposeDeltas(current, priorSnapshots);
        _windowStore.Append(current);

        _logger.LogInformation(
            "Operational trend observability: deltas composed. DeltaCount={DeltaCount}, SnapshotCount={SnapshotCount}",
            deltas.Count,
            Math.Min(priorSnapshots.Count + 1, OperationalTrendAggregation.MaxWindowSnapshots));

        return deltas;
    }

    private async Task<(OperationalTrendSnapshot Current, IReadOnlyList<OperationalTrendSnapshot> PriorSnapshots)> BuildCurrentSnapshotAsync(
        CancellationToken cancellationToken)
    {
        var priorSnapshots = _windowStore.GetSnapshots();

        await _compositionHub.BuildSnapshotAsync(cancellationToken).ConfigureAwait(false);

        var fingerprint = await _compositionHub.GetFingerprintSnapshotAsync(cancellationToken).ConfigureAwait(false);
        var runtimeProtection = await _compositionHub.GetRuntimeProtectionSnapshotAsync(cancellationToken).ConfigureAwait(false);
        var dashboard = await _compositionHub.GetDashboardSummaryAsync(cancellationToken).ConfigureAwait(false);
        var reconciliationWorkbench = await _compositionHub
            .GetReconciliationWorkbenchViewAsync(cancellationToken)
            .ConfigureAwait(false);
        var inventoryWorkbench = await _compositionHub.GetInventoryWorkbenchViewAsync(cancellationToken).ConfigureAwait(false);

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

        var current = OperationalTrendAggregation.BuildSnapshot(
            fingerprint,
            runtimeProtection,
            dashboard,
            replayPressure,
            replayStabilization,
            reconciliationWorkbench,
            inventoryWorkbench);

        return (current, priorSnapshots);
    }
}
