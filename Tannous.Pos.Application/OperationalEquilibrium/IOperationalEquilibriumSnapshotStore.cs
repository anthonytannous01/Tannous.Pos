namespace Tannous.Pos.Application.OperationalEquilibrium;

/// <summary>Bounded process-local equilibrium snapshot retention.</summary>
public interface IOperationalEquilibriumSnapshotStore
{
    int MaxSnapshots { get; }
    void Append(OperationalEquilibriumSnapshot snapshot);
    IReadOnlyList<OperationalEquilibriumSnapshot> GetSnapshots();
    void Clear();
}
