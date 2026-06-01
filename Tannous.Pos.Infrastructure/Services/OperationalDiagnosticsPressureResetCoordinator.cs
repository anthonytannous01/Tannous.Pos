using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.Audit;

using Tannous.Pos.Infrastructure.Services.OperationalDiagnosticsProjections;

namespace Tannous.Pos.Infrastructure.Services;

/// <summary>
/// Best-effort governance-only reset (pressure flags, caches, advisory counters).
/// GOVERNANCE: never mutates replay, reconciliation, or domain entities.
/// </summary>
public sealed class OperationalDiagnosticsPressureResetCoordinator : IOperationalDiagnosticsPressureResetCoordinator
{
    private readonly IOperationalResiliencePressureGovernanceReset _pressureReset;
    private readonly IOperationalResiliencePressureState _pressureState;
    private readonly IOperationalPressureLifecycleTracker _lifecycle;
    private readonly IOperationalDiagnosticsCache _cache;
    private readonly IOperationalDiagnosticsCacheTelemetry _telemetry;
    private readonly OperationalGovernanceSnapshotStore _snapshotStore;
    private readonly ILogger<OperationalDiagnosticsPressureResetCoordinator> _logger;

    public OperationalDiagnosticsPressureResetCoordinator(
        IOperationalResiliencePressureGovernanceReset pressureReset,
        IOperationalResiliencePressureState pressureState,
        IOperationalPressureLifecycleTracker lifecycle,
        IOperationalDiagnosticsCache cache,
        IOperationalDiagnosticsCacheTelemetry telemetry,
        OperationalGovernanceSnapshotStore snapshotStore,
        ILogger<OperationalDiagnosticsPressureResetCoordinator> logger)
    {
        _pressureReset = pressureReset;
        _pressureState = pressureState;
        _lifecycle = lifecycle;
        _cache = cache;
        _telemetry = telemetry;
        _snapshotStore = snapshotStore;
        _logger = logger;
    }

    public void ResetGovernanceState(bool clearDiagnosticsCaches = true)
    {
        var hadSticky = _lifecycle.GetSnapshot().StickyPressureDetected
                        || _pressureState.QueryDateRangeClamped
                        || _pressureState.QueryPageSizeClamped
                        || _pressureState.ForensicExportTruncated;

        try
        {
            _lifecycle.NoteGovernanceReset();
            _pressureReset.ResetGovernancePressureFlags();

            if (clearDiagnosticsCaches)
                _cache.RemoveAllDiagnosticsCaches();

            _snapshotStore.InvalidateAll();
            _telemetry.ResetGovernanceStabilizationBaseline();
            _telemetry.RecordStabilizationWindowReset();
            _telemetry.RecordPressureRecoveryCycle();
            _telemetry.RecordPressureLifecycleTransition();

            if (clearDiagnosticsCaches)
                _telemetry.RecordAdaptiveTtlRecovery();

            if (hadSticky)
                _telemetry.RecordStickyPressureRecovery();

            _lifecycle.NoteRecoveryTransition();
            _telemetry.RecordPressureConvergenceRecovery();

            _logger.LogInformation(
                "Operational pressure governance reset: governance state reset completed. ClearCaches={ClearCaches}, StickyRecovered={StickyRecovered}",
                clearDiagnosticsCaches,
                hadSticky);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Operational pressure governance reset: best-effort governance reset failed (non-fatal).");
        }
    }
}
