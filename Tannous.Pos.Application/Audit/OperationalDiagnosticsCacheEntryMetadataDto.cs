namespace Tannous.Pos.Application.Audit;

/// <summary>Diagnostics-only cache entry metadata (no cached values or payloads).</summary>
public sealed class OperationalDiagnosticsCacheEntryMetadataDto
{
    public string CacheKey { get; init; } = string.Empty;
    public string CacheKeyAlias { get; init; } = string.Empty;
    public string KeyDomain { get; init; } = string.Empty;
    public string Scope { get; init; } = OperationalDiagnosticsCacheScopes.Global;
    public string Category { get; init; } = string.Empty;
    public DateTime CreatedUtc { get; init; }
    public DateTime ExpiresUtc { get; init; }
    public DateTime? LastServedUtc { get; init; }
    public OperationalDiagnosticsCacheStaleRisk StaleRisk { get; init; }
    public int TtlSeconds { get; init; }
    public int AgeSeconds { get; init; }
    public int RemainingTtlSeconds { get; init; }
}
