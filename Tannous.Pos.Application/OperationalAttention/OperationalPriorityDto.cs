namespace Tannous.Pos.Application.OperationalAttention;

/// <summary>Deterministic operational priority for a bounded area.</summary>
public sealed class OperationalPriorityDto
{
    public string OperationalArea { get; init; } = string.Empty;
    public OperationalPriorityType PriorityType { get; init; }
    public OperationalEmphasisStrength PriorityStrength { get; init; }
    public OperationalUrgencyLevel OperationalUrgency { get; init; }
    public string StabilizationImportance { get; init; } = string.Empty;
    public string EscalationImportance { get; init; } = string.Empty;
    public string RecommendedOperatorFocus { get; init; } = string.Empty;
    public string OperatorInterpretation { get; init; } = string.Empty;
}
