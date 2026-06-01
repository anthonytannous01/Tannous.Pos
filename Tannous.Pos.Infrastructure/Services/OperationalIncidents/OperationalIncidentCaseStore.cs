using Tannous.Pos.Application.OperationalIncidents;

namespace Tannous.Pos.Infrastructure.Services.OperationalIncidents;

/// <summary>Process-local FIFO incident snapshot retention (max 8; not persisted).</summary>
public sealed class OperationalIncidentCaseStore : IOperationalIncidentCaseStore
{
    private readonly object _gate = new();
    private readonly Queue<OperationalIncidentCaseSnapshot> _snapshots = new();

    public int MaxSnapshots => OperationalIncidentAggregation.MaxStoredSnapshots;

    public void Append(OperationalIncidentCaseSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        lock (_gate)
        {
            while (_snapshots.Count >= MaxSnapshots)
                _snapshots.Dequeue();

            _snapshots.Enqueue(snapshot);
        }
    }

    public IReadOnlyList<OperationalIncidentCaseSnapshot> GetSnapshots()
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
