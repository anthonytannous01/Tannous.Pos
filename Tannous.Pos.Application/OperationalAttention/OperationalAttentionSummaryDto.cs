namespace Tannous.Pos.Application.OperationalAttention;

/// <summary>Condensed operational attention summary for operator focus coordination.</summary>
public sealed class OperationalAttentionSummaryDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public string HighestPriorityConcern { get; init; } = string.Empty;
    public string DominantEscalationFocus { get; init; } = string.Empty;
    public string DominantStabilizationFocus { get; init; } = string.Empty;
    public string StrongestOperationalEmphasis { get; init; } = string.Empty;
    public OperationalAttentionState OperationalAttentionState { get; init; }
    public string OperatorAttentionLevel { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
}
