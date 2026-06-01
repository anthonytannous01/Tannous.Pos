namespace Tannous.Pos.Application.OperationalPatterns;

/// <summary>Deterministic correlation between operational patterns.</summary>
public sealed class OperationalPatternCorrelationDto
{
    public string SourcePattern { get; init; } = string.Empty;
    public string RelatedPattern { get; init; } = string.Empty;
    public OperationalPatternConfidence CorrelationStrength { get; init; }
    public IReadOnlyList<string> SharedOperationalAreas { get; init; } = Array.Empty<string>();
    public string SharedPropagationCharacteristics { get; init; } = string.Empty;
    public string SharedRecoveryBehavior { get; init; } = string.Empty;
    public string OperatorInterpretation { get; init; } = string.Empty;
}
