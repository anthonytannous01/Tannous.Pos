using Tannous.Pos.Application.OperationalSituationRoom;
using Tannous.Pos.Application.OperationalCognition;
using Tannous.Pos.Infrastructure.Services.OperationalCognition;

namespace Tannous.Pos.Infrastructure.Services.OperationalSituationRoom;

/// <summary>Process-local FIFO situation snapshot retention (max 8; not persisted).</summary>
public sealed class OperationalSituationSnapshotStore : IOperationalSituationSnapshotStore
{
    private readonly BoundedFifoSnapshotStore<OperationalSituationSnapshot> _store = new(
        OperationalCognitionSnapshotLimits.MaxStoredSnapshots);

    public int MaxSnapshots => _store.MaxSnapshots;

    public void Append(OperationalSituationSnapshot snapshot) => _store.Append(snapshot);

    public IReadOnlyList<OperationalSituationSnapshot> GetSnapshots() => _store.GetSnapshots();

    public void Clear() => _store.Clear();
}
