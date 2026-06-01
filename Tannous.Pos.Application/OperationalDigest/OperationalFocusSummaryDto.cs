namespace Tannous.Pos.Application.OperationalDigest;

/// <summary>Condensed operational focus summary for digest synthesis.</summary>
public sealed class OperationalFocusSummaryDto
{
    public string HighestPriorityArea { get; init; } = string.Empty;
    public string HighestRiskEscalation { get; init; } = string.Empty;
    public string StrongestRecoverySignal { get; init; } = string.Empty;
    public string DominantConstraint { get; init; } = string.Empty;
    public string RecommendedOperatorSequence { get; init; } = string.Empty;
    public string StabilizationConfidence { get; init; } = string.Empty;
}
