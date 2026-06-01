namespace Tannous.Pos.Application.OperationalTimeline;

/// <summary>Process-local bounded timeline event retention (FIFO; not persisted).</summary>
public interface IOperationalTimelineWindowStore
{
    int MaxEvents { get; }

    void Append(OperationalTimelineEventRecord timelineEvent);

    IReadOnlyList<OperationalTimelineEventRecord> GetEvents();

    OperationalTimelineCaptureSnapshot? GetLastCapture();

    void SetLastCapture(OperationalTimelineCaptureSnapshot capture);

    void Clear();
}
