namespace Tannous.Pos.Application.OperationalSimulation;

/// <summary>Deterministic operational leverage interpretation for an area.</summary>
public sealed class OperationalLeveragePointDto
{
    public string Area { get; init; } = string.Empty;
    public OperationalLeverageStrength LeverageStrength { get; init; }
    public string RecoveryInfluence { get; init; } = string.Empty;
    public string StabilizationInfluence { get; init; } = string.Empty;
    public string EscalationInfluence { get; init; } = string.Empty;
    public string DownstreamImpact { get; init; } = string.Empty;
    public string OperatorPriorityReason { get; init; } = string.Empty;
}
