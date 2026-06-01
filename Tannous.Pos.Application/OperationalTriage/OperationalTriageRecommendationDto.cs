namespace Tannous.Pos.Application.OperationalTriage;

/// <summary>Deterministic triage investigation recommendation (no workflow automation).</summary>
public sealed class OperationalTriageRecommendationDto
{
    public int Priority { get; init; }
    public string Title { get; init; } = string.Empty;
    public string RecommendedAction { get; init; } = string.Empty;
    public string RecommendedRoute { get; init; } = string.Empty;
    public OperationalTriagePriority PriorityBand { get; init; }
}
