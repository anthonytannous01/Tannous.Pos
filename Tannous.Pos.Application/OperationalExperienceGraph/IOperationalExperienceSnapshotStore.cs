namespace Tannous.Pos.Application.OperationalExperienceGraph;

/// <summary>Process-local bounded experience snapshot retention (FIFO; not persisted).</summary>
public interface IOperationalExperienceSnapshotStore
{
    int MaxSnapshots { get; }

    void Append(OperationalExperienceSnapshot snapshot);

    IReadOnlyList<OperationalExperienceSnapshot> GetSnapshots();

    void Clear();
}
