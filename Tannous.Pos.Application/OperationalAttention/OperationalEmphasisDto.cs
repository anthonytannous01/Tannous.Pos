namespace Tannous.Pos.Application.OperationalAttention;

/// <summary>Deterministic operational emphasis in a bounded area.</summary>
public sealed class OperationalEmphasisDto
{
    public string OperationalArea { get; init; } = string.Empty;
    public OperationalEmphasisStrength EmphasisStrength { get; init; }
    public string ReinforcingSignals { get; init; } = string.Empty;
    public string EscalationPressure { get; init; } = string.Empty;
    public string RecoveryPressure { get; init; } = string.Empty;
    public string StabilizationPressure { get; init; } = string.Empty;
    public string OperatorInterpretation { get; init; } = string.Empty;
}
