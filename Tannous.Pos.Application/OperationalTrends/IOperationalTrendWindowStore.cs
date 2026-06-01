namespace Tannous.Pos.Application.OperationalTrends;

/// <summary>Process-local bounded trend snapshot retention (FIFO; not persisted).</summary>
public interface IOperationalTrendWindowStore
{
    int MaxSnapshots { get; }

    void Append(OperationalTrendSnapshot snapshot);

    IReadOnlyList<OperationalTrendSnapshot> GetSnapshots();

    void Clear();
}
