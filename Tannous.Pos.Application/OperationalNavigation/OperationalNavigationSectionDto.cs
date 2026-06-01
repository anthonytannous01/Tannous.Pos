namespace Tannous.Pos.Application.OperationalNavigation;

/// <summary>Operator navigation section with recommended route and action.</summary>
public sealed class OperationalNavigationSectionDto
{
    public string SectionName { get; init; } = string.Empty;
    public OperationalNavigationSeverity Severity { get; init; }
    public OperationalNavigationState State { get; init; }
    public string RecommendedRoute { get; init; } = string.Empty;
    public string RecommendedAction { get; init; } = string.Empty;
    public string AttentionSummary { get; init; } = string.Empty;
}
