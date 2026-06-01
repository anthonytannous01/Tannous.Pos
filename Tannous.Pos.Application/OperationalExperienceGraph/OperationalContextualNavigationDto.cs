namespace Tannous.Pos.Application.OperationalExperienceGraph;

/// <summary>Contextual navigation guidance for the current operational focus.</summary>
public sealed class OperationalContextualNavigationDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public string CurrentOperationalFocus { get; init; } = string.Empty;
    public string RecommendedNextSurface { get; init; } = string.Empty;
    public IReadOnlyList<string> RelatedOperationalAreas { get; init; } = Array.Empty<string>();
    public string DominantReason { get; init; } = string.Empty;
    public OperationalTraversalPriority InvestigationPriority { get; init; }
    public OperationalNavigationStrength NavigationStrength { get; init; }
    public string OperatorInterpretation { get; init; } = string.Empty;
    public string ExperienceNote { get; init; } =
        "Advisory deterministic contextual navigation composed from existing operational intelligence. Not workflow routing, AI recommendations, or frontend navigation.";
}
