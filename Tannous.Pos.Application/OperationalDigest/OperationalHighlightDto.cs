namespace Tannous.Pos.Application.OperationalDigest;

/// <summary>Deterministic high-signal operational highlight.</summary>
public sealed class OperationalHighlightDto
{
    public OperationalHighlightType HighlightType { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public OperationalDigestSeverity Severity { get; init; }
    public string RelatedArea { get; init; } = string.Empty;
    public string RecommendedAttention { get; init; } = string.Empty;
    public string OperatorInterpretation { get; init; } = string.Empty;
}
