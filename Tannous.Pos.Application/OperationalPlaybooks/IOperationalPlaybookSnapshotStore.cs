namespace Tannous.Pos.Application.OperationalPlaybooks;

/// <summary>Process-local bounded playbook snapshot retention (FIFO; not persisted).</summary>
public interface IOperationalPlaybookSnapshotStore
{
    int MaxSnapshots { get; }

    void Append(OperationalPlaybookSnapshot snapshot);

    IReadOnlyList<OperationalPlaybookSnapshot> GetSnapshots();

    void Clear();
}
