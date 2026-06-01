namespace Tannous.Pos.Application.OperationalSituationRoom;

/// <summary>Platform-wide operational risk concentration by area.</summary>
public sealed class OperationalRiskConcentrationDto
{
    public string Area { get; init; } = string.Empty;
    public OperationalExecutiveSeverity Severity { get; init; }
    public int IncidentContribution { get; init; }
    public int PropagationContribution { get; init; }
    public string RecoveryImpact { get; init; } = string.Empty;
    public string StabilizationRisk { get; init; } = string.Empty;
    public bool OperatorAttentionRequired { get; init; }
}
