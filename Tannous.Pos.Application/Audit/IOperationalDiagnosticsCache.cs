namespace Tannous.Pos.Application.Audit;

/// <summary>
/// In-process operational diagnostics cache (summaries/projections only).
/// Factories run sequentially on the caller's async context; no parallel orchestration.
/// </summary>
public interface IOperationalDiagnosticsCache
{
    Task<OperationalDiagnosticsCacheEnvelope<T>> GetOrCreateAsync<T>(
        string cacheKey,
        string category,
        TimeSpan ttl,
        Func<CancellationToken, Task<T>> factory,
        bool bypass = false,
        CancellationToken cancellationToken = default) where T : class;

    void Remove(string cacheKey, string category);

    void RemoveByScope(string category, string scope, string? scopeId = null);

    void RemoveByPrefix(string keyPrefix);

    void RemoveAllDiagnosticsCaches();

    bool TryGetMetadata(string cacheKey, string category, out OperationalDiagnosticsCacheEntryMetadataDto metadata);

    bool TryGetEnvelope<T>(string cacheKey, string category, out OperationalDiagnosticsCacheEnvelope<T>? envelope)
        where T : class;

    IReadOnlyList<OperationalDiagnosticsCacheEntryMetadataDto> GetActiveEntryMetadata();

    /// <summary>Diagnostics-only metadata (no cached values or envelope bodies).</summary>
    IReadOnlyList<OperationalDiagnosticsCacheEntryMetadataDto> GetDiagnosticsEntryMetadata();
}
