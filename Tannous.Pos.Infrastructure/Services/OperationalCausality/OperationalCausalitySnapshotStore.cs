using Tannous.Pos.Application.OperationalCausality;
using Tannous.Pos.Application.OperationalCognition;
using Tannous.Pos.Infrastructure.Services.OperationalCognition;

namespace Tannous.Pos.Infrastructure.Services.OperationalCausality;

/// <summary>Process-local FIFO causality snapshot retention (max 8; not persisted).</summary>
public sealed class OperationalCausalitySnapshotStore : IOperationalCausalitySnapshotStore
{
    private readonly BoundedFifoSnapshotStore<OperationalCausalitySnapshot> _store = new(
        OperationalCognitionSnapshotLimits.MaxStoredSnapshots);

    public int MaxSnapshots => _store.MaxSnapshots;

    public void Append(OperationalCausalitySnapshot snapshot) => _store.Append(snapshot);

    public IReadOnlyList<OperationalCausalitySnapshot> GetSnapshots() => _store.GetSnapshots();

    public void Clear() => _store.Clear();
}
