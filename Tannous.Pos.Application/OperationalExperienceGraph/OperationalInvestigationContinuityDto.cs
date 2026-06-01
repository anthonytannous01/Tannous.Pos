namespace Tannous.Pos.Application.OperationalExperienceGraph;

/// <summary>Investigation continuity across related operational surfaces.</summary>
public sealed class OperationalInvestigationContinuityDto
{
    public string InvestigationTheme { get; init; } = string.Empty;
    public IReadOnlyList<string> RelatedSurfaces { get; init; } = Array.Empty<string>();
    public string DominantArea { get; init; } = string.Empty;
    public string EscalationAlignment { get; init; } = string.Empty;
    public string RecoveryAlignment { get; init; } = string.Empty;
    public string RecommendedOperatorFlow { get; init; } = string.Empty;
    public string OperationalConsistency { get; init; } = string.Empty;
}
