namespace Tannous.Pos.Application.OperationalIncidents;

/// <summary>Advisory incident stabilization outlook for operator focus.</summary>
public sealed class OperationalIncidentOutlookDto
{
    public OperationalIncidentDirection RecoveryDirection { get; init; }
    public string StabilizationLikelihood { get; init; } = string.Empty;
    public string EscalationRisk { get; init; } = string.Empty;
    public OperationalIncidentConfidence OperationalConfidence { get; init; }
    public string RecommendedOperatorFocus { get; init; } = string.Empty;
}
