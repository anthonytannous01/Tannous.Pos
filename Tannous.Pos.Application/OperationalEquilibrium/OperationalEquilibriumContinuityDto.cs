namespace Tannous.Pos.Application.OperationalEquilibrium;

/// <summary>Bounded equilibrium continuity across snapshot window.</summary>
public sealed class OperationalEquilibriumContinuityDto
{
    public string DominantEquilibriumShift { get; init; } = string.Empty;
    public string StabilizationBalanceConsistency { get; init; } = string.Empty;
    public string EscalationBalanceConsistency { get; init; } = string.Empty;
    public string RecoveryEquilibriumAlignment { get; init; } = string.Empty;
    public string SystemicCoordinationAlignment { get; init; } = string.Empty;
    public string OperatorInterpretation { get; init; } = string.Empty;
}
