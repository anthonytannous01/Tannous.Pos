namespace Tannous.Pos.Application.OperationalNavigation;

/// <summary>Deterministic operator navigation recommendation (no workflow automation).</summary>
public sealed class OperationalNavigationRecommendationDto
{
    public int Priority { get; init; }
    public string Title { get; init; } = string.Empty;
    public string RecommendedAction { get; init; } = string.Empty;
    public string RelativeRoute { get; init; } = string.Empty;
    public OperationalNavigationSeverity Severity { get; init; }
}
