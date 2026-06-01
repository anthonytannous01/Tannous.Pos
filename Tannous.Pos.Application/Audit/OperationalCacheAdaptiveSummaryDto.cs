namespace Tannous.Pos.Application.Audit;

public sealed class OperationalCacheAdaptiveSummaryDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public OperationalCacheReadinessState ReadinessState { get; init; }
    public OperationalCacheTtlMode DominantTtlMode { get; init; }
    public int WarmCandidateCount { get; init; }
    public IReadOnlyList<string> WarmestCategories { get; init; } = Array.Empty<string>();
    public long WarmRecommendations { get; init; }
    public long RepeatedColdMisses { get; init; }
    public long AdaptiveTtlReductions { get; init; }
    public IReadOnlyDictionary<string, int> EffectiveTtlSecondsByCategory { get; init; } =
        new Dictionary<string, int>(StringComparer.Ordinal);
    public IReadOnlyDictionary<string, string> TtlModeByCategory { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);
    public string GovernanceNote { get; init; } = string.Empty;
    public IReadOnlyList<string> ReasonCodes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> TriggerSignals { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> GovernanceNotes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> RecommendedActions { get; init; } = Array.Empty<string>();
}
