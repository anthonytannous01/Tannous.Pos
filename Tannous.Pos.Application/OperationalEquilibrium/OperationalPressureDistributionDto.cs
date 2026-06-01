namespace Tannous.Pos.Application.OperationalEquilibrium;

/// <summary>Operational pressure distribution across a bounded area.</summary>
public sealed class OperationalPressureDistributionDto
{
    public string OperationalArea { get; init; } = string.Empty;
    public string PressureWeight { get; init; } = string.Empty;
    public string EscalationDistribution { get; init; } = string.Empty;
    public string StabilizationDistribution { get; init; } = string.Empty;
    public string RecoveryDistribution { get; init; } = string.Empty;
    public string OperatorInterpretation { get; init; } = string.Empty;
}
