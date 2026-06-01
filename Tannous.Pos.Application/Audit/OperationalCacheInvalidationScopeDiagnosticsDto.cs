namespace Tannous.Pos.Application.Audit;

public sealed class OperationalCacheInvalidationScopeDiagnosticsDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public int ActiveScopedKeyCount { get; init; }
    public long ScopedInvalidations { get; init; }
    public long ScopedInvalidationRecoveries { get; init; }
    public double ScopeChurnRatio { get; init; }
    public IReadOnlyDictionary<string, int> ScopedEntriesByCategory { get; init; } =
        new Dictionary<string, int>(StringComparer.Ordinal);
    public IReadOnlyList<OperationalCacheScopedEntrySurvivabilityDto> OldestScopedEntries { get; init; } =
        Array.Empty<OperationalCacheScopedEntrySurvivabilityDto>();
    public IReadOnlyList<string> ReasonCodes { get; init; } = Array.Empty<string>();
    public string GovernanceNote { get; init; } = string.Empty;
}
