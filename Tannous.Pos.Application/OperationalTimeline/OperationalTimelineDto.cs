namespace Tannous.Pos.Application.OperationalTimeline;

/// <summary>
/// Operator operational timeline (read-only; bounded process-local chronology).
/// NON-GOAL: not durable history; not event sourcing; no persistence.
/// </summary>
public sealed class OperationalTimelineDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public int EventCount { get; init; }
    public int MaxEvents { get; init; } = OperationalTimelineAggregation.MaxTimelineEvents;
    public IReadOnlyList<OperationalTimelineEventDto> Events { get; init; } = Array.Empty<OperationalTimelineEventDto>();
    public IReadOnlyList<OperationalTimelineAttentionDto> AttentionItems { get; init; } = Array.Empty<OperationalTimelineAttentionDto>();
    public string Summary { get; init; } = string.Empty;
    public string TimelineNote { get; init; } =
        "Advisory process-local operational timeline composed from existing diagnostics. Events are bounded, non-persistent, and not a durable audit history.";
}
