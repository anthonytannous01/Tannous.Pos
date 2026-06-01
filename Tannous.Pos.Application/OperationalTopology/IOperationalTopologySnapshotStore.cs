namespace Tannous.Pos.Application.OperationalTopology;

/// <summary>Process-local bounded topology snapshot retention (FIFO; not persisted).</summary>
public interface IOperationalTopologySnapshotStore
{
    int MaxSnapshots { get; }

    void Append(OperationalTopologySnapshot snapshot);

    IReadOnlyList<OperationalTopologySnapshot> GetSnapshots();

    void Clear();
}
