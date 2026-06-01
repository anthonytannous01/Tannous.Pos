namespace Tannous.Pos.Application.Audit;

public sealed class OperationalCacheCardinalitySnapshotDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public OperationalCacheCardinalityClassification Classification { get; init; }
    public int ActiveEntryCount { get; init; }
    public int MaxCacheEntryCount { get; init; }
    public int ActiveScopedKeyCount { get; init; }
    public int GlobalEntryCount { get; init; }
    public double SaturationRatio { get; init; }
    public double ScopedEntryRatio { get; init; }
    public IReadOnlyDictionary<string, int> EntriesByScope { get; init; } =
        new Dictionary<string, int>(StringComparer.Ordinal);
    public IReadOnlyDictionary<string, int> EntriesByCategory { get; init; } =
        new Dictionary<string, int>(StringComparer.Ordinal);
    public string GovernanceNote { get; init; } = string.Empty;
}
