namespace Tannous.Pos.Application.OperationalSimulation;

/// <summary>Process-local bounded simulation snapshot retention (FIFO; not persisted).</summary>
public interface IOperationalSimulationSnapshotStore
{
    int MaxSnapshots { get; }

    void Append(OperationalSimulationSnapshot snapshot);

    IReadOnlyList<OperationalSimulationSnapshot> GetSnapshots();

    void Clear();
}
