namespace Tannous.Pos.Application.OperationalTimeline;

/// <summary>Deterministic correlated event sequence (heuristic only; not causal inference).</summary>
public sealed class OperationalTimelineCorrelationDto
{
    public string CorrelationLabel { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public OperationalTimelineSeverity Severity { get; init; }
    public IReadOnlyList<string> RelatedCategories { get; init; } = Array.Empty<string>();
    public string SuggestedRoute { get; init; } = string.Empty;
}
