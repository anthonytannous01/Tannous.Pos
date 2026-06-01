namespace Tannous.Pos.Application.OperationalCausality;

/// <summary>Process-local bounded causality snapshot retention (FIFO; not persisted).</summary>
public interface IOperationalCausalitySnapshotStore
{
    int MaxSnapshots { get; }

    void Append(OperationalCausalitySnapshot snapshot);

    IReadOnlyList<OperationalCausalitySnapshot> GetSnapshots();

    void Clear();
}
