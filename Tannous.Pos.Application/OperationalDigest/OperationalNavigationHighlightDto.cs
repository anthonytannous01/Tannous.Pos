namespace Tannous.Pos.Application.OperationalDigest;

/// <summary>Condensed contextual navigation highlight.</summary>
public sealed class OperationalNavigationHighlightDto
{
    public string RecommendedSurface { get; init; } = string.Empty;
    public string NavigationReason { get; init; } = string.Empty;
    public string RelatedOperationalTheme { get; init; } = string.Empty;
    public OperationalAttentionPriority InvestigationPriority { get; init; }
    public string ExpectedOperatorOutcome { get; init; } = string.Empty;
    public string OperatorInterpretation { get; init; } = string.Empty;
}
