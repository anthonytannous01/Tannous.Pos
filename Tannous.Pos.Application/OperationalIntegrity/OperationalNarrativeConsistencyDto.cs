namespace Tannous.Pos.Application.OperationalIntegrity;

/// <summary>Dominant operational narrative consistency across intelligence layers.</summary>
public sealed class OperationalNarrativeConsistencyDto
{
    public string DominantNarrative { get; init; } = string.Empty;
    public IReadOnlyList<string> SupportingLayers { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ContradictingLayers { get; init; } = Array.Empty<string>();
    public OperationalConsistencyDirection StabilityDirection { get; init; }
    public string RecoveryAlignment { get; init; } = string.Empty;
    public string OperationalConfidence { get; init; } = string.Empty;
}
