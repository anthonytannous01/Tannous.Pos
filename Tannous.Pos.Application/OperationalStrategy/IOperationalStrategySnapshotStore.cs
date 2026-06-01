namespace Tannous.Pos.Application.OperationalStrategy;

/// <summary>Bounded process-local strategy snapshot retention.</summary>
public interface IOperationalStrategySnapshotStore
{
    int MaxSnapshots { get; }
    void Append(OperationalStrategySnapshot snapshot);
    IReadOnlyList<OperationalStrategySnapshot> GetSnapshots();
    void Clear();
}
