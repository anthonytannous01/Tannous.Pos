namespace Tannous.Pos.Application.OperationalExperienceGraph;

/// <summary>Platform-wide operational experience navigation summary.</summary>
public sealed class OperationalExperienceSummaryDto
{
    public string DominantOperationalFlow { get; init; } = string.Empty;
    public string MostConnectedSurface { get; init; } = string.Empty;
    public string HighestPriorityTraversal { get; init; } = string.Empty;
    public string RecoveryNavigationAlignment { get; init; } = string.Empty;
    public string EscalationNavigationAlignment { get; init; } = string.Empty;
    public string OperatorAttentionLevel { get; init; } = string.Empty;
}
