namespace Tannous.Pos.Application.OperationalConvergence;

/// <summary>Deterministic operational signal divergence between intelligence layers.</summary>
public sealed class OperationalDivergenceDto
{
    public string DivergenceId { get; init; } = string.Empty;
    public string OperationalArea { get; init; } = string.Empty;
    public IReadOnlyList<string> ConflictingLayers { get; init; } = Array.Empty<string>();
    public OperationalDivergenceType DivergenceType { get; init; }
    public OperationalAmbiguityLevel DivergenceSeverity { get; init; }
    public string OperationalRisk { get; init; } = string.Empty;
    public string RecommendedOperatorFocus { get; init; } = string.Empty;
}
