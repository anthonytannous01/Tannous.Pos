namespace Tannous.Pos.Application.OperationalPlaybooks;

/// <summary>Ordered operational response step within a playbook sequence.</summary>
public sealed class OperationalResponseStepDto
{
    public int SequenceOrder { get; init; }
    public string Area { get; init; } = string.Empty;
    public string Objective { get; init; } = string.Empty;
    public string RecommendedFocus { get; init; } = string.Empty;
    public string ExpectedOutcome { get; init; } = string.Empty;
    public OperationalGuidanceSeverity EscalationRisk { get; init; }
    public string StabilizationContribution { get; init; } = string.Empty;
    public string OperatorInstruction { get; init; } = string.Empty;
}
