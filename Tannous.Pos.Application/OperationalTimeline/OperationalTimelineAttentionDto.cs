namespace Tannous.Pos.Application.OperationalTimeline;

/// <summary>Operator timeline attention item (advisory guidance only).</summary>
public sealed class OperationalTimelineAttentionDto
{
    public int Priority { get; init; }
    public OperationalTimelineSeverity Severity { get; init; }
    public OperationalTimelineCategory Category { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;
    public string SuggestedRoute { get; init; } = string.Empty;
}
