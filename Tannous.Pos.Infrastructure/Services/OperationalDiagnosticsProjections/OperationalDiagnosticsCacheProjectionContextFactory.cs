using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Audit.Governance;

namespace Tannous.Pos.Infrastructure.Services.OperationalDiagnosticsProjections;

internal sealed class OperationalDiagnosticsCacheProjectionContextFactory
{
    private readonly OperationalGovernanceSnapshotStore _snapshotStore;
    private readonly OperationalGovernanceProjectionMemoizer _memoizer;
    private readonly IOperationalDiagnosticsCache _cache;
    private readonly IOperationalDiagnosticsCacheTelemetry _telemetry;
    private readonly IOperationalResiliencePressureState _pressureState;

    public OperationalDiagnosticsCacheProjectionContextFactory(
        OperationalGovernanceSnapshotStore snapshotStore,
        OperationalGovernanceProjectionMemoizer memoizer,
        IOperationalDiagnosticsCache cache,
        IOperationalDiagnosticsCacheTelemetry telemetry,
        IOperationalResiliencePressureState pressureState)
    {
        _snapshotStore = snapshotStore;
        _memoizer = memoizer;
        _cache = cache;
        _telemetry = telemetry;
        _pressureState = pressureState;
    }

    public OperationalGovernanceSnapshotAccess AcquireSnapshot(
        OperationalGovernanceProfile profile = OperationalGovernanceProfile.Standard) =>
        _memoizer.Acquire(_snapshotStore, profile);

    public OperationalGovernanceCompositionContext BuildFullContext(
        OperationalGovernanceProfile profile = OperationalGovernanceProfile.Standard) =>
        AcquireSnapshot(profile).Composition.Context;

    public IReadOnlyList<OperationalDiagnosticsCacheEntryMetadataDto> GetEntries() =>
        _cache.GetDiagnosticsEntryMetadata();

    public OperationalDiagnosticsCacheTelemetrySnapshotDto GetTelemetry() =>
        _memoizer.GetTelemetry(_telemetry);

    public IOperationalResiliencePressureState PressureState => _pressureState;
}
