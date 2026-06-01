namespace Tannous.Pos.Application.OperationalTimeline;

/// <summary>
/// Bounded process-local timeline event (classifications only).
/// NON-GOAL: no payloads, entity IDs, receipts, exports, or cache metadata.
/// </summary>
public sealed class OperationalTimelineEventRecord
{
    public DateTime OccurredAtUtc { get; init; }
    public OperationalTimelineCategory Category { get; init; }
    public OperationalTimelineSeverity Severity { get; init; }
    public OperationalTimelineDirection Direction { get; init; }
    public string Summary { get; init; } = string.Empty;
    public string CorrelationLabel { get; init; } = string.Empty;
    public string SuggestedRoute { get; init; } = string.Empty;
}
