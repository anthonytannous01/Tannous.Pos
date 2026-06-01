namespace Tannous.Pos.Application.OperationalEquilibrium;

/// <summary>Systemic balance analysis for a bounded operational area.</summary>
public sealed class OperationalSystemicBalanceDto
{
    public string OperationalArea { get; init; } = string.Empty;
    public OperationalBalanceStrength BalanceStrength { get; init; }
    public string StabilizationPressure { get; init; } = string.Empty;
    public string EscalationPressure { get; init; } = string.Empty;
    public string RecoveryPressure { get; init; } = string.Empty;
    public string CoordinationBalance { get; init; } = string.Empty;
    public string OperatorInterpretation { get; init; } = string.Empty;
}
