using Tannous.Pos.Application.OperationalTimeline;

namespace Tannous.Pos.Infrastructure.Services.OperationalTimeline;

/// <summary>Process-local FIFO timeline event retention (max 25; not persisted).</summary>
public sealed class OperationalTimelineWindowStore : IOperationalTimelineWindowStore
{
    private readonly object _gate = new();
    private readonly Queue<OperationalTimelineEventRecord> _events = new();
    private OperationalTimelineCaptureSnapshot? _lastCapture;

    public int MaxEvents => OperationalTimelineAggregation.MaxTimelineEvents;

    public void Append(OperationalTimelineEventRecord timelineEvent)
    {
        ArgumentNullException.ThrowIfNull(timelineEvent);

        lock (_gate)
        {
            while (_events.Count >= MaxEvents)
                _events.Dequeue();

            _events.Enqueue(timelineEvent);
        }
    }

    public IReadOnlyList<OperationalTimelineEventRecord> GetEvents()
    {
        lock (_gate)
            return _events.ToList();
    }

    public OperationalTimelineCaptureSnapshot? GetLastCapture()
    {
        lock (_gate)
            return _lastCapture;
    }

    public void SetLastCapture(OperationalTimelineCaptureSnapshot capture)
    {
        ArgumentNullException.ThrowIfNull(capture);

        lock (_gate)
            _lastCapture = capture;
    }

    public void Clear()
    {
        lock (_gate)
        {
            _events.Clear();
            _lastCapture = null;
        }
    }
}
