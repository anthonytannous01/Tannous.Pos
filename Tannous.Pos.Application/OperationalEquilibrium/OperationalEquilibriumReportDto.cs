namespace Tannous.Pos.Application.OperationalEquilibrium;

/// <summary>Deterministic operational equilibrium and systemic balance report.</summary>
public sealed class OperationalEquilibriumReportDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public OperationalEquilibriumState EquilibriumState { get; init; }
    public string StabilizationBalance { get; init; } = string.Empty;
    public string EscalationBalance { get; init; } = string.Empty;
    public string RecoveryEquilibriumStrength { get; init; } = string.Empty;
    public OperationalStrainLevel SystemicStrainLevel { get; init; }
    public string HighestImbalanceArea { get; init; } = string.Empty;
    public IReadOnlyList<OperationalSystemicBalanceDto> SystemicBalances { get; init; } =
        Array.Empty<OperationalSystemicBalanceDto>();
    public IReadOnlyList<OperationalImbalanceDto> Imbalances { get; init; } =
        Array.Empty<OperationalImbalanceDto>();
    public IReadOnlyList<OperationalPressureDistributionDto> PressureDistributions { get; init; } =
        Array.Empty<OperationalPressureDistributionDto>();
    public OperationalEquilibriumContinuityDto EquilibriumContinuity { get; init; } = new();
    public string OperatorSummary { get; init; } = string.Empty;
    public string EquilibriumNote { get; init; } =
        "Advisory deterministic operational equilibrium from bounded cognition continuity. Not control theory, optimization, or probabilistic equilibrium scoring.";
}
