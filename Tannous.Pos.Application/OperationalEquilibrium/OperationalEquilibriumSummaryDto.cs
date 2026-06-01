namespace Tannous.Pos.Application.OperationalEquilibrium;

/// <summary>Condensed operational equilibrium summary.</summary>
public sealed class OperationalEquilibriumSummaryDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public OperationalEquilibriumState DominantEquilibriumState { get; init; }
    public string HighestImbalancePressure { get; init; } = string.Empty;
    public string StrongestStabilizationBalance { get; init; } = string.Empty;
    public string WeakestRecoveryEquilibrium { get; init; } = string.Empty;
    public OperationalEquilibriumDirection OperationalEquilibriumDirection { get; init; }
    public string OperatorAttentionLevel { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
}
