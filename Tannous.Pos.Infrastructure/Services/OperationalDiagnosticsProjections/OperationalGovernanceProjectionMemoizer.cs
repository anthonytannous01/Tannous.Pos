using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Audit.Governance;

namespace Tannous.Pos.Infrastructure.Services.OperationalDiagnosticsProjections;

/// <summary>
/// Request-scoped memoization for governance projection composition (no static state; not persisted).
/// </summary>
internal sealed class OperationalGovernanceProjectionMemoizer
{
    private readonly Dictionary<OperationalGovernanceProfile, OperationalGovernanceSnapshotAccess> _snapshotAccess =
        new();
    private OperationalDiagnosticsCacheTelemetrySnapshotDto? _telemetrySnapshot;

    public OperationalGovernanceSnapshotAccess Acquire(
        OperationalGovernanceSnapshotStore snapshotStore,
        OperationalGovernanceProfile profile = OperationalGovernanceProfile.Standard)
    {
        if (_snapshotAccess.TryGetValue(profile, out var cached))
            return cached;

        var access = snapshotStore.Acquire(profile);
        _snapshotAccess[profile] = access;
        return access;
    }

    public OperationalDiagnosticsCacheTelemetrySnapshotDto GetTelemetry(
        IOperationalDiagnosticsCacheTelemetry telemetry)
    {
        _telemetrySnapshot ??= OperationalGovernanceTelemetryAccess.CaptureSnapshot(telemetry);
        return _telemetrySnapshot;
    }

    public void Reset()
    {
        _snapshotAccess.Clear();
        _telemetrySnapshot = null;
    }
}
