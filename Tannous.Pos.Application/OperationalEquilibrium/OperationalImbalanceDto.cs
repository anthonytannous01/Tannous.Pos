namespace Tannous.Pos.Application.OperationalEquilibrium;

/// <summary>Deterministic operational imbalance signal.</summary>
public sealed class OperationalImbalanceDto
{
    public string ImbalanceId { get; init; } = string.Empty;
    public string OperationalArea { get; init; } = string.Empty;
    public OperationalImbalanceType ImbalanceType { get; init; }
    public OperationalBalanceStrength ImbalanceSeverity { get; init; }
    public string StrainConcentration { get; init; } = string.Empty;
    public string StabilizationRisk { get; init; } = string.Empty;
    public string RecommendedOperatorFocus { get; init; } = string.Empty;
}
