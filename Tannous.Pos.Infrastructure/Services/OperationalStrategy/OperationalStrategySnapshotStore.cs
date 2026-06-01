using Tannous.Pos.Application.OperationalStrategy;
using Tannous.Pos.Application.OperationalCognition;
using Tannous.Pos.Infrastructure.Services.OperationalCognition;

namespace Tannous.Pos.Infrastructure.Services.OperationalStrategy;

/// <summary>Process-local FIFO strategy snapshot retention (max 8; not persisted).</summary>
public sealed class OperationalStrategySnapshotStore : IOperationalStrategySnapshotStore
{
    private readonly BoundedFifoSnapshotStore<OperationalStrategySnapshot> _store = new(
        OperationalCognitionSnapshotLimits.MaxStoredSnapshots);

    public int MaxSnapshots => _store.MaxSnapshots;

    public void Append(OperationalStrategySnapshot snapshot) => _store.Append(snapshot);

    public IReadOnlyList<OperationalStrategySnapshot> GetSnapshots() => _store.GetSnapshots();

    public void Clear() => _store.Clear();
}
