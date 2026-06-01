namespace Tannous.Pos.Application.OperationalTimeline;

/// <summary>Operator-facing timeline event (read-only; advisory chronology).</summary>
public sealed class OperationalTimelineEventDto
{
    public DateTime OccurredAtUtc { get; init; }
    public OperationalTimelineCategory Category { get; init; }
    public OperationalTimelineSeverity Severity { get; init; }
    public OperationalTimelineDirection Direction { get; init; }
    public string Summary { get; init; } = string.Empty;
    public string CorrelationLabel { get; init; } = string.Empty;
    public string SuggestedRoute { get; init; } = string.Empty;
}
