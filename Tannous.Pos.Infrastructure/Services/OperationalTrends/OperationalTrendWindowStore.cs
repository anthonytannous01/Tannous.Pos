using Tannous.Pos.Application.OperationalTrends;

namespace Tannous.Pos.Infrastructure.Services.OperationalTrends;

/// <summary>Process-local FIFO trend snapshot retention (max 3; not persisted).</summary>
public sealed class OperationalTrendWindowStore : IOperationalTrendWindowStore
{
    private readonly object _gate = new();
    private readonly Queue<OperationalTrendSnapshot> _snapshots = new();

    public int MaxSnapshots => OperationalTrendAggregation.MaxWindowSnapshots;

    public void Append(OperationalTrendSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        lock (_gate)
        {
            while (_snapshots.Count >= MaxSnapshots)
                _snapshots.Dequeue();

            _snapshots.Enqueue(snapshot);
        }
    }

    public IReadOnlyList<OperationalTrendSnapshot> GetSnapshots()
    {
        lock (_gate)
            return _snapshots.ToList();
    }

    public void Clear()
    {
        lock (_gate)
            _snapshots.Clear();
    }
}
