namespace Tannous.Pos.Application.OperationalConvergence;

/// <summary>Process-local bounded convergence snapshot retention (FIFO; not persisted).</summary>
public interface IOperationalConvergenceSnapshotStore
{
    int MaxSnapshots { get; }

    void Append(OperationalConvergenceSnapshot snapshot);

    IReadOnlyList<OperationalConvergenceSnapshot> GetSnapshots();

    void Clear();
}
