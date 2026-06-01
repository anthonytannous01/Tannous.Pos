namespace Tannous.Pos.Application.Audit;

public sealed class OperationalCacheScopeDiagnosticsDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public int ActiveScopedKeyCount { get; init; }
    public long TotalInvalidations { get; init; }
    public long InvalidationsByScopedKeys { get; init; }
    public double ScopeChurnRatio { get; init; }
    public IReadOnlyDictionary<string, int> ScopeDistributionByCategory { get; init; } =
        new Dictionary<string, int>(StringComparer.Ordinal);
    public IReadOnlyList<OperationalCacheScopedEntrySurvivabilityDto> OldestScopedEntries { get; init; } =
        Array.Empty<OperationalCacheScopedEntrySurvivabilityDto>();
    public string GovernanceNote { get; init; } = string.Empty;
}

public sealed class OperationalCacheScopedEntrySurvivabilityDto
{
    public string Category { get; init; } = string.Empty;
    public string Scope { get; init; } = string.Empty;
    public string CacheKeyAlias { get; init; } = string.Empty;
    public int AgeSeconds { get; init; }
    public int RemainingTtlSeconds { get; init; }
}
