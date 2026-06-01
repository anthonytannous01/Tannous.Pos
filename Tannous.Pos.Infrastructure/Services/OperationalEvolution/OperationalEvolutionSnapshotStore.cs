using Tannous.Pos.Application.OperationalEvolution;
using Tannous.Pos.Application.OperationalCognition;
using Tannous.Pos.Infrastructure.Services.OperationalCognition;

namespace Tannous.Pos.Infrastructure.Services.OperationalEvolution;

/// <summary>Process-local FIFO evolution snapshot retention (max 8; not persisted).</summary>
public sealed class OperationalEvolutionSnapshotStore : IOperationalEvolutionSnapshotStore
{
    private readonly BoundedFifoSnapshotStore<OperationalEvolutionSnapshot> _store = new(
        OperationalCognitionSnapshotLimits.MaxStoredSnapshots);

    public int MaxSnapshots => _store.MaxSnapshots;

    public void Append(OperationalEvolutionSnapshot snapshot) => _store.Append(snapshot);

    public IReadOnlyList<OperationalEvolutionSnapshot> GetSnapshots() => _store.GetSnapshots();

    public void Clear() => _store.Clear();
}
