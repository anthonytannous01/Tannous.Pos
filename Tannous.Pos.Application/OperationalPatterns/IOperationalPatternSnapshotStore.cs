namespace Tannous.Pos.Application.OperationalPatterns;

/// <summary>Process-local bounded pattern snapshot retention (FIFO; not persisted).</summary>
public interface IOperationalPatternSnapshotStore
{
    int MaxSnapshots { get; }

    void Append(OperationalPatternSnapshot snapshot);

    IReadOnlyList<OperationalPatternSnapshot> GetSnapshots();

    void Clear();
}
