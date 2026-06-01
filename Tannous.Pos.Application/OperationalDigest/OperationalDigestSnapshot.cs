namespace Tannous.Pos.Application.OperationalDigest;

/// <summary>Lightweight process-local digest snapshot for short-term continuity.</summary>
public sealed class OperationalDigestSnapshot
{
    public DateTime GeneratedAtUtc { get; init; }
    public OperationalDigestState DigestState { get; init; }
    public string DominantOperationalStory { get; init; } = string.Empty;
    public string DominantRiskArea { get; init; } = string.Empty;
    public string RecommendedOperatorFocus { get; init; } = string.Empty;
    public int HighlightCount { get; init; }
}
