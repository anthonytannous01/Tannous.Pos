namespace Tannous.Pos.Application.OperationalSituationRoom;

/// <summary>Process-local bounded situation snapshot retention (FIFO; not persisted).</summary>
public interface IOperationalSituationSnapshotStore
{
    int MaxSnapshots { get; }

    void Append(OperationalSituationSnapshot snapshot);

    IReadOnlyList<OperationalSituationSnapshot> GetSnapshots();

    void Clear();
}
