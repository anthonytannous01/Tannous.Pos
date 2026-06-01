using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.OperationalComposition;
using Tannous.Pos.Application.OperationalReplayWorkbench;

namespace Tannous.Pos.Infrastructure.Services;

/// <summary>
/// Operator-facing replay pressure workbench via shared operational composition hub (read-only; no nested workbench service calls).
/// </summary>
public sealed class OperationalReplayWorkbenchService : IOperationalReplayWorkbenchService
{
    private readonly IOperationalReadCompositionHub _compositionHub;
    private readonly ILogger<OperationalReplayWorkbenchService> _logger;

    public OperationalReplayWorkbenchService(
        IOperationalReadCompositionHub compositionHub,
        ILogger<OperationalReplayWorkbenchService> logger)
    {
        _compositionHub = compositionHub;
        _logger = logger;
    }

    public async Task<OperationalReplayWorkbenchDto> GetPressureWorkbenchAsync(
        CancellationToken cancellationToken = default)
    {
        var resilience = await _compositionHub.GetResilienceSummaryAsync(cancellationToken).ConfigureAwait(false);
        var reconciliation = await _compositionHub.GetReconciliationSummaryAsync(cancellationToken).ConfigureAwait(false);
        var incidents = await _compositionHub.GetIncidentsSummaryAsync(cancellationToken).ConfigureAwait(false);
        var alerts = await _compositionHub.GetAlertSummaryAsync(cancellationToken).ConfigureAwait(false);
        var governanceOverview = await _compositionHub.GetGovernanceOverviewAsync(cancellationToken).ConfigureAwait(false);
        var runtimeSnapshot = await _compositionHub.GetRuntimeProtectionSnapshotAsync(cancellationToken).ConfigureAwait(false);
        var dashboard = await _compositionHub.GetDashboardSummaryAsync(cancellationToken).ConfigureAwait(false);
        var reconciliationWorkbench = await _compositionHub
            .GetReconciliationWorkbenchViewAsync(cancellationToken)
            .ConfigureAwait(false);
        var inventoryWorkbench = await _compositionHub.GetInventoryWorkbenchViewAsync(cancellationToken).ConfigureAwait(false);

        var runtimeSignals = new OperationalReplayRuntimeSignals
        {
            ProtectiveContainmentActive = runtimeSnapshot.FailsafeActive
                || dashboard.Pressure.ProtectiveModeActive,
            RuntimeSaturationIndicated = string.Equals(
                    runtimeSnapshot.TelemetrySaturationLevel,
                    "Elevated",
                    StringComparison.OrdinalIgnoreCase)
                || string.Equals(
                    runtimeSnapshot.TelemetrySaturationLevel,
                    "Saturated",
                    StringComparison.OrdinalIgnoreCase)
                || dashboard.Pressure.RuntimeSaturationIndicated
        };

        var pressureSummary = OperationalReplayWorkbenchAggregation.ComposePressureSummary(
            resilience,
            reconciliation,
            alerts,
            incidents,
            dashboard,
            reconciliationWorkbench,
            runtimeSignals);
        var stabilization = OperationalReplayWorkbenchAggregation.ComposeStabilization(
            resilience,
            reconciliation,
            dashboard,
            reconciliationWorkbench,
            inventoryWorkbench,
            runtimeSignals,
            pressureSummary);
        var hotspots = OperationalReplayWorkbenchAggregation.ComposeHotspots(
            resilience,
            reconciliation,
            alerts,
            incidents,
            governanceOverview,
            dashboard,
            reconciliationWorkbench,
            inventoryWorkbench);
        var recoveryConfidence = OperationalReplayWorkbenchAggregation.ComposeRecoveryConfidence(
            resilience,
            dashboard,
            reconciliationWorkbench,
            stabilization,
            governanceOverview,
            runtimeSignals);
        var attentionItems = OperationalReplayWorkbenchAggregation.ComposeAttentionItems(
            resilience,
            reconciliation,
            alerts,
            incidents,
            governanceOverview,
            dashboard,
            reconciliationWorkbench,
            inventoryWorkbench,
            stabilization,
            pressureSummary);

        _logger.LogInformation(
            "Operational replay workbench observability: pressure workbench composed. Instability={Instability}, Hotspots={Hotspots}, AttentionItems={AttentionItems}, RecoveryConfidence={RecoveryConfidence}",
            pressureSummary.InstabilityLevel,
            hotspots.Count,
            attentionItems.Count,
            recoveryConfidence.Confidence);

        return new OperationalReplayWorkbenchDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            PressureSummary = pressureSummary,
            Stabilization = stabilization,
            Hotspots = hotspots,
            RecoveryConfidence = recoveryConfidence,
            AttentionItems = attentionItems
        };
    }
}
