namespace Tannous.Pos.Application.OperationalNavigation;

/// <summary>
/// Central operator navigation index (read-only; advisory routing only).
/// NON-GOAL: not workflow automation; not governance infrastructure; no mutations.
/// </summary>
public sealed class OperationalNavigationIndexDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public OperationalNavigationSeverity OverallSeverity { get; init; }
    public OperationalNavigationState OverallState { get; init; }
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyList<OperationalNavigationSectionDto> Sections { get; init; } = Array.Empty<OperationalNavigationSectionDto>();
    public IReadOnlyList<OperationalNavigationRecommendationDto> Recommendations { get; init; } = Array.Empty<OperationalNavigationRecommendationDto>();
    public IReadOnlyList<OperationalNavigationAttentionDto> AttentionItems { get; init; } = Array.Empty<OperationalNavigationAttentionDto>();
    public string NavigationNote { get; init; } =
        "Advisory operational navigation index composed from existing diagnostics. Routes indicate where to inspect — no automated actions are performed.";
}
