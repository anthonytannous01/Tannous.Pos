namespace Tannous.Pos.Application.OperationalIntegrity;

/// <summary>Process-local bounded integrity snapshot retention (FIFO; not persisted).</summary>
public interface IOperationalIntegritySnapshotStore
{
    int MaxSnapshots { get; }

    void Append(OperationalIntegritySnapshot snapshot);

    IReadOnlyList<OperationalIntegritySnapshot> GetSnapshots();

    void Clear();
}
