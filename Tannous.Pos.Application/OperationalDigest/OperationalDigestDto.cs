namespace Tannous.Pos.Application.OperationalDigest;

/// <summary>Deterministic condensed operational intelligence digest.</summary>
public sealed class OperationalDigestDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public OperationalDigestState DigestState { get; init; }
    public string DominantOperationalStory { get; init; } = string.Empty;
    public string DominantRiskArea { get; init; } = string.Empty;
    public string RecoveryDirection { get; init; } = string.Empty;
    public string StabilizationPriority { get; init; } = string.Empty;
    public string EscalationPressure { get; init; } = string.Empty;
    public string IntegrityState { get; init; } = string.Empty;
    public string RecommendedOperatorFocus { get; init; } = string.Empty;
    public string ExecutiveDigest { get; init; } = string.Empty;
    public string OperatorDigest { get; init; } = string.Empty;
    public OperationalFocusSummaryDto FocusSummary { get; init; } = new();
    public IReadOnlyList<OperationalHighlightDto> OperationalHighlights { get; init; } = Array.Empty<OperationalHighlightDto>();
    public IReadOnlyList<OperationalNavigationHighlightDto> NavigationHighlights { get; init; } =
        Array.Empty<OperationalNavigationHighlightDto>();
    public string DigestNote { get; init; } =
        "Advisory deterministic operational condensation composed from existing intelligence layers. Not dashboards, BI reporting, or AI summarization.";
}
