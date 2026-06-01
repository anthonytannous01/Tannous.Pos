namespace Tannous.Pos.Application.OperationalConvergence;

/// <summary>Condensed operational convergence summary for operator attention.</summary>
public sealed class OperationalConvergenceSummaryDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public string DominantConvergenceArea { get; init; } = string.Empty;
    public string HighestDivergencePressure { get; init; } = string.Empty;
    public string StrongestReinforcement { get; init; } = string.Empty;
    public string HighestAmbiguityConcentration { get; init; } = string.Empty;
    public OperationalConvergenceState OperationalStabilityState { get; init; }
    public string OperatorAttentionLevel { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
}
