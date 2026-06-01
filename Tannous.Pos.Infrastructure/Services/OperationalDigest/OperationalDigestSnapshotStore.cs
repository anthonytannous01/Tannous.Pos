using Tannous.Pos.Application.OperationalDigest;
using Tannous.Pos.Application.OperationalCognition;
using Tannous.Pos.Infrastructure.Services.OperationalCognition;

namespace Tannous.Pos.Infrastructure.Services.OperationalDigest;

/// <summary>Process-local FIFO digest snapshot retention (max 8; not persisted).</summary>
public sealed class OperationalDigestSnapshotStore : IOperationalDigestSnapshotStore
{
    private readonly BoundedFifoSnapshotStore<OperationalDigestSnapshot> _store = new(
        OperationalCognitionSnapshotLimits.MaxStoredSnapshots);

    public int MaxSnapshots => _store.MaxSnapshots;

    public void Append(OperationalDigestSnapshot snapshot) => _store.Append(snapshot);

    public IReadOnlyList<OperationalDigestSnapshot> GetSnapshots() => _store.GetSnapshots();

    public void Clear() => _store.Clear();
}
