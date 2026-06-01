using Tannous.Pos.Application.OperationalAttention;
using Tannous.Pos.Application.OperationalCognition;
using Tannous.Pos.Infrastructure.Services.OperationalCognition;

namespace Tannous.Pos.Infrastructure.Services.OperationalAttention;

/// <summary>Process-local FIFO attention snapshot retention (max 8; not persisted).</summary>
public sealed class OperationalAttentionSnapshotStore : IOperationalAttentionSnapshotStore
{
    private readonly BoundedFifoSnapshotStore<OperationalAttentionSnapshot> _store = new(
        OperationalCognitionSnapshotLimits.MaxStoredSnapshots);

    public int MaxSnapshots => _store.MaxSnapshots;

    public void Append(OperationalAttentionSnapshot snapshot) => _store.Append(snapshot);

    public IReadOnlyList<OperationalAttentionSnapshot> GetSnapshots() => _store.GetSnapshots();

    public void Clear() => _store.Clear();
}
