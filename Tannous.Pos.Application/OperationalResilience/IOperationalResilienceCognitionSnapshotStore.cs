namespace Tannous.Pos.Application.OperationalResilience;

/// <summary>Process-local bounded resilience cognition snapshot retention (FIFO; not persisted).</summary>
public interface IOperationalResilienceCognitionSnapshotStore
{
    int MaxSnapshots { get; }

    void Append(OperationalResilienceCognitionSnapshot snapshot);

    IReadOnlyList<OperationalResilienceCognitionSnapshot> GetSnapshots();

    void Clear();
}
