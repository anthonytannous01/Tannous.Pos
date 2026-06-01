namespace Tannous.Pos.Application.OperationalStrategy;

/// <summary>Strategic alignment analysis for a bounded operational area.</summary>
public sealed class OperationalStrategicAlignmentDto
{
    public string OperationalArea { get; init; } = string.Empty;
    public OperationalAlignmentState AlignmentStrength { get; init; }
    public string ReinforcingOperationalSignals { get; init; } = string.Empty;
    public string ContradictingOperationalSignals { get; init; } = string.Empty;
    public string StrategicConsistency { get; init; } = string.Empty;
    public string OperatorInterpretation { get; init; } = string.Empty;
}
