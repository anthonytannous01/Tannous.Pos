namespace Tannous.Pos.Application.Audit;

public sealed class OperationalDiagnosticsCacheDiagnosticsSummaryDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public int ActiveEntryCount { get; init; }
    public int MaxCacheEntryCount { get; init; }
    public int? OldestEntryAgeSeconds { get; init; }
    public int? NewestEntryAgeSeconds { get; init; }
    public long TotalInvalidations { get; init; }
    public DateTime? LastInvalidationUtc { get; init; }
    public int ActiveScopedKeyCount { get; init; }
    public IReadOnlyDictionary<string, int> EntriesByScope { get; init; } =
        new Dictionary<string, int>(StringComparer.Ordinal);
    public IReadOnlyList<OperationalDiagnosticsCacheEntryMetadataDto> Entries { get; init; } =
        Array.Empty<OperationalDiagnosticsCacheEntryMetadataDto>();
    public IReadOnlyDictionary<string, int> EntriesByCategory { get; init; } =
        new Dictionary<string, int>(StringComparer.Ordinal);
    public IReadOnlyDictionary<string, int> CategoryTtlSeconds { get; init; } =
        new Dictionary<string, int>(StringComparer.Ordinal);
    public string GovernanceNote { get; init; } = string.Empty;
}
