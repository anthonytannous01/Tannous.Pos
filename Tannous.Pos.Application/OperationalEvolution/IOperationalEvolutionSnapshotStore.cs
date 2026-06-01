namespace Tannous.Pos.Application.OperationalEvolution;

/// <summary>Process-local bounded evolution snapshot retention (FIFO; not persisted).</summary>
public interface IOperationalEvolutionSnapshotStore
{
    int MaxSnapshots { get; }

    void Append(OperationalEvolutionSnapshot snapshot);

    IReadOnlyList<OperationalEvolutionSnapshot> GetSnapshots();

    void Clear();
}
