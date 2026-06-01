namespace Tannous.Pos.Application.OperationalAttention;

/// <summary>Deterministic attention coordination signal for operator focus routing.</summary>
public sealed class OperationalAttentionCoordinationDto
{
    public string CoordinationId { get; init; } = string.Empty;
    public string DominantOperationalConcern { get; init; } = string.Empty;
    public string AttentionRouting { get; init; } = string.Empty;
    public string EscalationWeight { get; init; } = string.Empty;
    public string StabilizationWeight { get; init; } = string.Empty;
    public string InvestigationWeight { get; init; } = string.Empty;
    public string OperatorSummary { get; init; } = string.Empty;
}
