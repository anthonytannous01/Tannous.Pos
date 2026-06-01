namespace Tannous.Pos.Infrastructure.Services.OperationalCognition;

/// <summary>Process-local bounded FIFO snapshot retention — thread-safe, not persisted.</summary>
public sealed class BoundedFifoSnapshotStore<T>
    where T : class
{
    private readonly object _gate = new();
    private readonly Queue<T> _snapshots = new();

    public BoundedFifoSnapshotStore(int maxSnapshots)
    {
        if (maxSnapshots <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxSnapshots));

        MaxSnapshots = maxSnapshots;
    }

    public int MaxSnapshots { get; }

    public void Append(T snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        lock (_gate)
        {
            while (_snapshots.Count >= MaxSnapshots)
                _snapshots.Dequeue();

            _snapshots.Enqueue(snapshot);
        }
    }

    public IReadOnlyList<T> GetSnapshots()
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
