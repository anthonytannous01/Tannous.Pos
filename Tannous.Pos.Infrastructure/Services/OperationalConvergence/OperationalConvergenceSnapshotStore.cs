using Tannous.Pos.Application.OperationalConvergence;
using Tannous.Pos.Application.OperationalCognition;
using Tannous.Pos.Infrastructure.Services.OperationalCognition;

namespace Tannous.Pos.Infrastructure.Services.OperationalConvergence;

/// <summary>Process-local FIFO convergence snapshot retention (max 8; not persisted).</summary>
public sealed class OperationalConvergenceSnapshotStore : IOperationalConvergenceSnapshotStore
{
    private readonly BoundedFifoSnapshotStore<OperationalConvergenceSnapshot> _store = new(
        OperationalCognitionSnapshotLimits.MaxStoredSnapshots);

    public int MaxSnapshots => _store.MaxSnapshots;

    public void Append(OperationalConvergenceSnapshot snapshot) => _store.Append(snapshot);

    public IReadOnlyList<OperationalConvergenceSnapshot> GetSnapshots() => _store.GetSnapshots();

    public void Clear() => _store.Clear();
}
