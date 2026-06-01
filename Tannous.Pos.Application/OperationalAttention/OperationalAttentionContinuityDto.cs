namespace Tannous.Pos.Application.OperationalAttention;

/// <summary>Bounded operational attention continuity across snapshot window.</summary>
public sealed class OperationalAttentionContinuityDto
{
    public string DominantAttentionShift { get; init; } = string.Empty;
    public string PriorityConsistency { get; init; } = string.Empty;
    public string EscalationAttentionAlignment { get; init; } = string.Empty;
    public string StabilizationAttentionAlignment { get; init; } = string.Empty;
    public string InvestigationFocusAlignment { get; init; } = string.Empty;
    public string OperatorInterpretation { get; init; } = string.Empty;
}
