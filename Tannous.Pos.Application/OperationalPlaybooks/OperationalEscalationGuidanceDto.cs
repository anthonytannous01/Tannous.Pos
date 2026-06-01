namespace Tannous.Pos.Application.OperationalPlaybooks;

/// <summary>Deterministic escalation handling guidance.</summary>
public sealed class OperationalEscalationGuidanceDto
{
    public OperationalEscalationType EscalationType { get; init; }
    public OperationalGuidanceSeverity Severity { get; init; }
    public string TriggerCondition { get; init; } = string.Empty;
    public string RecommendedOperatorFocus { get; init; } = string.Empty;
    public OperationalStabilizationPriority ContainmentPriority { get; init; }
    public OperationalStabilizationPriority RecoveryPriority { get; init; }
    public string OperatorInterpretation { get; init; } = string.Empty;
}
