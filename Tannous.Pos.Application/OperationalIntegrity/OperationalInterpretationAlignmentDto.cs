namespace Tannous.Pos.Application.OperationalIntegrity;

/// <summary>Deterministic alignment between two operational intelligence layers.</summary>
public sealed class OperationalInterpretationAlignmentDto
{
    public string SourceLayer { get; init; } = string.Empty;
    public string TargetLayer { get; init; } = string.Empty;
    public OperationalAlignmentType AlignmentType { get; init; }
    public string AlignmentStrength { get; init; } = string.Empty;
    public string SharedOperationalDirection { get; init; } = string.Empty;
    public string SharedDominantArea { get; init; } = string.Empty;
    public string SharedRecoveryInterpretation { get; init; } = string.Empty;
    public string OperatorInterpretation { get; init; } = string.Empty;
}
