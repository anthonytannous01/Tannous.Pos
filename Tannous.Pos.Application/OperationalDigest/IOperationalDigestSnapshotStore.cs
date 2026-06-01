namespace Tannous.Pos.Application.OperationalDigest;

/// <summary>Process-local bounded digest snapshot retention (FIFO; not persisted).</summary>
public interface IOperationalDigestSnapshotStore
{
    int MaxSnapshots { get; }

    void Append(OperationalDigestSnapshot snapshot);

    IReadOnlyList<OperationalDigestSnapshot> GetSnapshots();

    void Clear();
}
