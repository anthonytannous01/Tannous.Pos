using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Infrastructure.Services.OperationalDiagnosticsProjections;

namespace Tannous.Pos.Infrastructure.Services;

/// <summary>
/// In-process IMemoryCache for operational diagnostics summaries only.
/// GOVERNANCE / NON-GOAL: no payload bodies; no forensic exports; no EF entities; TTL-only expiry.
/// </summary>
public sealed class OperationalDiagnosticsCacheService : IOperationalDiagnosticsCache
{
    private readonly IMemoryCache _memoryCache;
    private readonly IOperationalDiagnosticsCacheTelemetry _telemetry;
    private readonly OperationalGovernanceSnapshotStore _snapshotStore;
    private readonly ILogger<OperationalDiagnosticsCacheService> _logger;
    private readonly ConcurrentDictionary<string, OperationalDiagnosticsCacheEntryMetadataDto> _metadata =
        new(StringComparer.Ordinal);

    public OperationalDiagnosticsCacheService(
        IMemoryCache memoryCache,
        IOperationalDiagnosticsCacheTelemetry telemetry,
        OperationalGovernanceSnapshotStore snapshotStore,
        ILogger<OperationalDiagnosticsCacheService> logger)
    {
        _memoryCache = memoryCache;
        _telemetry = telemetry;
        _snapshotStore = snapshotStore;
        _logger = logger;
    }

    public async Task<OperationalDiagnosticsCacheEnvelope<T>> GetOrCreateAsync<T>(
        string cacheKey,
        string category,
        TimeSpan ttl,
        Func<CancellationToken, Task<T>> factory,
        bool bypass = false,
        CancellationToken cancellationToken = default) where T : class
    {
        var storageKey = BuildStorageKey(category, cacheKey);
        var keyAlias = BuildKeyAlias(cacheKey);

        if (bypass)
        {
            _telemetry.RecordBypass(category);
            _logger.LogInformation(
                "Operational cache observability: cache bypass. Category={Category}, CacheKeyAlias={CacheKeyAlias}, TtlSeconds={TtlSeconds}",
                category,
                keyAlias,
                (int)ttl.TotalSeconds);

            var bypassValue = await factory(cancellationToken).ConfigureAwait(false);
            var bypassServed = DateTime.UtcNow;
            return Wrap(bypassValue, cacheKey, category, bypassServed, bypassServed.Add(ttl), bypassServed);
        }

        if (_memoryCache.TryGetValue(storageKey, out OperationalDiagnosticsCacheEnvelope<T>? cached) && cached != null)
        {
            cached.ServedUtc = DateTime.UtcNow;
            UpdateLastServed(storageKey, cached.ServedUtc);

            var staleRisk = cached.StaleRisk;
            _telemetry.RecordHit(category);

            if (staleRisk is OperationalDiagnosticsCacheStaleRisk.Aging
                or OperationalDiagnosticsCacheStaleRisk.NearExpiry
                or OperationalDiagnosticsCacheStaleRisk.Expired)
            {
                _telemetry.RecordStaleServe(category, staleRisk);
                _logger.LogWarning(
                    "Operational stale snapshot risk: serving cached diagnostics. Category={Category}, CacheKeyAlias={CacheKeyAlias}, StaleRisk={StaleRisk}, AgeSeconds={AgeSeconds}, RemainingTtlSeconds={RemainingTtlSeconds}",
                    category,
                    keyAlias,
                    staleRisk,
                    (int)cached.Age.TotalSeconds,
                    (int)cached.RemainingTtl.TotalSeconds);
            }

            _logger.LogInformation(
                "Operational cache observability: cache hit. Category={Category}, CacheKeyAlias={CacheKeyAlias}, StaleRisk={StaleRisk}, TtlSeconds={TtlSeconds}, AgeSeconds={AgeSeconds}",
                category,
                keyAlias,
                staleRisk,
                (int)ttl.TotalSeconds,
                (int)cached.Age.TotalSeconds);

            return cached;
        }

        _telemetry.RecordMiss(category);
        _logger.LogInformation(
            "Operational cache observability: cache miss. Category={Category}, CacheKeyAlias={CacheKeyAlias}, TtlSeconds={TtlSeconds}",
            category,
            keyAlias,
            (int)ttl.TotalSeconds);

        var value = await factory(cancellationToken).ConfigureAwait(false);
        var createdUtc = DateTime.UtcNow;
        var expiresUtc = createdUtc.Add(ttl);
        var envelope = Wrap(value, cacheKey, category, createdUtc, expiresUtc, createdUtc);

        var options = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(ttl)
            .SetSize(1);

        options.RegisterPostEvictionCallback((key, _, _, _) =>
        {
            if (key is string evictedKey)
                _metadata.TryRemove(evictedKey, out _);
        });

        _memoryCache.Set(storageKey, envelope, options);
        RegisterMetadata(storageKey, cacheKey, category, createdUtc, expiresUtc, createdUtc);

        _logger.LogDebug(
            "Operational scoped cache: scoped key registered. Category={Category}, CacheKeyAlias={CacheKeyAlias}",
            category,
            keyAlias);

        return envelope;
    }

    public void Remove(string cacheKey, string category) =>
        RemoveInternal(cacheKey, category);

    public void RemoveByScope(string category, string scope, string? scopeId = null)
    {
        var domain = MapCategoryToDomain(category);
        var key = OperationalDiagnosticsCacheKeyFactory.Build(domain, scope, scopeId);
        RemoveInternal(key, category, scopedInvalidation: true);
    }

    public void RemoveByPrefix(string keyPrefix)
    {
        if (string.IsNullOrWhiteSpace(keyPrefix))
            return;

        var matches = _metadata
            .Where(kvp => kvp.Value.CacheKey.StartsWith(keyPrefix, StringComparison.Ordinal))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var storageKey in matches)
        {
            if (!_metadata.TryRemove(storageKey, out var meta))
                continue;

            _memoryCache.Remove(storageKey);
            var scoped = !string.Equals(meta.Scope, OperationalDiagnosticsCacheScopes.Global, StringComparison.Ordinal);
            RecordInvalidationSideEffects(meta, scoped);
            _telemetry.RecordInvalidation(meta.Category, 1, scoped);
            LogInvalidation(meta.Category, meta.CacheKey, meta.CacheKeyAlias, "prefix");
        }

        if (matches.Count > 0)
            _snapshotStore.InvalidateAll();
    }

    public void RemoveAllDiagnosticsCaches()
    {
        var storageKeys = _metadata.Keys.ToList();
        var categoriesRemoved = new HashSet<string>(StringComparer.Ordinal);
        foreach (var storageKey in storageKeys)
        {
            if (!_metadata.TryRemove(storageKey, out var meta))
                continue;

            _memoryCache.Remove(storageKey);
            categoriesRemoved.Add(meta.Category);
            RecordInvalidationSideEffects(meta, scoped: false);
            _telemetry.RecordInvalidation(meta.Category, 1);
            LogInvalidation(meta.Category, meta.CacheKey, meta.CacheKeyAlias, "all");
        }

        if (categoriesRemoved.Count > 1)
        {
            _telemetry.RecordCrossCategoryInvalidation(categoriesRemoved.Count);
            _telemetry.RecordPropagationDetection();
        }

        if (storageKeys.Count > 0)
        {
            _telemetry.RecordConsistencyRecoveryCycle();
            _snapshotStore.InvalidateAll();
        }
    }

    private void RemoveInternal(string cacheKey, string category, bool scopedInvalidation = false)
    {
        var storageKey = BuildStorageKey(category, cacheKey);
        _memoryCache.Remove(storageKey);
        if (_metadata.TryRemove(storageKey, out var meta))
        {
            var scoped = scopedInvalidation
                || !string.Equals(meta.Scope, OperationalDiagnosticsCacheScopes.Global, StringComparison.Ordinal);
            RecordInvalidationSideEffects(meta, scoped);
            _telemetry.RecordInvalidation(category, 1, scoped);
            LogInvalidation(category, cacheKey, meta.CacheKeyAlias, "single");
            _snapshotStore.InvalidateAll();
        }
    }

    private void RecordInvalidationSideEffects(
        OperationalDiagnosticsCacheEntryMetadataDto meta,
        bool scoped)
    {
        if (meta.StaleRisk != OperationalDiagnosticsCacheStaleRisk.Fresh)
            _telemetry.RecordFreshnessRecovery();

        if (scoped)
            _telemetry.RecordScopedInvalidationRecovery();
    }

    private void LogInvalidation(string category, string cacheKey, string keyAlias, string mode)
    {
        _logger.LogInformation(
            "Operational cache invalidation: cache removal executed. Category={Category}, CacheKeyAlias={CacheKeyAlias}, Mode={Mode}",
            category,
            keyAlias,
            mode);

        _logger.LogDebug(
            "Operational scoped cache: invalidation key resolved. Category={Category}, KeyPattern={KeyPattern}",
            category,
            cacheKey);
    }

    public bool TryGetMetadata(string cacheKey, string category, out OperationalDiagnosticsCacheEntryMetadataDto metadata)
    {
        var storageKey = BuildStorageKey(category, cacheKey);
        return _metadata.TryGetValue(storageKey, out metadata!);
    }

    public bool TryGetEnvelope<T>(string cacheKey, string category, out OperationalDiagnosticsCacheEnvelope<T>? envelope)
        where T : class
    {
        var storageKey = BuildStorageKey(category, cacheKey);
        if (_memoryCache.TryGetValue(storageKey, out OperationalDiagnosticsCacheEnvelope<T>? cached) && cached != null)
        {
            envelope = cached;
            return true;
        }

        envelope = null;
        return false;
    }

    public IReadOnlyList<OperationalDiagnosticsCacheEntryMetadataDto> GetActiveEntryMetadata() =>
        GetDiagnosticsEntryMetadata();

    /// <summary>Immutable metadata projections for diagnostics endpoints (no cached values).</summary>
    public IReadOnlyList<OperationalDiagnosticsCacheEntryMetadataDto> GetDiagnosticsEntryMetadata()
    {
        var now = DateTime.UtcNow;
        return _metadata.Values
            .Select(meta => ProjectDiagnosticsMetadata(meta, now))
            .OrderBy(m => m.Category, StringComparer.Ordinal)
            .ThenBy(m => m.CacheKeyAlias, StringComparer.Ordinal)
            .ToList();
    }

    private void RegisterMetadata(
        string storageKey,
        string cacheKey,
        string category,
        DateTime createdUtc,
        DateTime expiresUtc,
        DateTime servedUtc)
    {
        _metadata[storageKey] = CreateMetadataRecord(cacheKey, category, createdUtc, expiresUtc, servedUtc);
    }

    private void UpdateLastServed(string storageKey, DateTime servedUtc)
    {
        if (!_metadata.TryGetValue(storageKey, out var meta))
            return;

        _metadata[storageKey] = CreateMetadataRecord(
            meta.CacheKey,
            meta.Category,
            meta.CreatedUtc,
            meta.ExpiresUtc,
            servedUtc);
    }

    private static OperationalDiagnosticsCacheEntryMetadataDto CreateMetadataRecord(
        string cacheKey,
        string category,
        DateTime createdUtc,
        DateTime expiresUtc,
        DateTime servedUtc)
    {
        var now = DateTime.UtcNow;
        var ttlSeconds = (int)Math.Max(0, (expiresUtc - createdUtc).TotalSeconds);
        var ageSeconds = (int)Math.Max(0, (now - createdUtc).TotalSeconds);
        var remainingTtlSeconds = (int)Math.Max(0, (expiresUtc - now).TotalSeconds);

        var (domain, scope) = ParseKeySegments(cacheKey);

        return new OperationalDiagnosticsCacheEntryMetadataDto
        {
            CacheKey = cacheKey,
            CacheKeyAlias = BuildKeyAlias(cacheKey),
            KeyDomain = domain,
            Scope = scope,
            Category = category,
            CreatedUtc = createdUtc,
            ExpiresUtc = expiresUtc,
            LastServedUtc = servedUtc,
            StaleRisk = OperationalDiagnosticsCacheStaleRiskClassifier.Classify(createdUtc, expiresUtc, now),
            TtlSeconds = ttlSeconds,
            AgeSeconds = ageSeconds,
            RemainingTtlSeconds = remainingTtlSeconds
        };
    }

    private static (string Domain, string Scope) ParseKeySegments(string cacheKey)
    {
        var parts = cacheKey.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            return ("unknown", OperationalDiagnosticsCacheScopes.Global);

        var scope = parts.Length > 1 ? parts[1] : OperationalDiagnosticsCacheScopes.Global;
        return (parts[0], scope);
    }

    private static string MapCategoryToDomain(string category) =>
        category switch
        {
            OperationalDiagnosticsCacheCategories.ResilienceMetrics =>
                OperationalDiagnosticsCacheKeyConstants.ResilienceDomain,
            OperationalDiagnosticsCacheCategories.ReconciliationSummary =>
                OperationalDiagnosticsCacheKeyConstants.ReconciliationDomain,
            OperationalDiagnosticsCacheCategories.IncidentGroups or OperationalDiagnosticsCacheCategories.IncidentSummary =>
                OperationalDiagnosticsCacheKeyConstants.IncidentDomain,
            OperationalDiagnosticsCacheCategories.AlertSignals =>
                OperationalDiagnosticsCacheKeyConstants.AlertSignalsSegment,
            OperationalDiagnosticsCacheCategories.AlertSummary =>
                OperationalDiagnosticsCacheKeyConstants.AlertSummarySegment,
            _ => category.ToLowerInvariant()
        };

    private static OperationalDiagnosticsCacheEntryMetadataDto ProjectDiagnosticsMetadata(
        OperationalDiagnosticsCacheEntryMetadataDto source,
        DateTime now)
    {
        var ageSeconds = (int)Math.Max(0, (now - source.CreatedUtc).TotalSeconds);
        var remainingTtlSeconds = (int)Math.Max(0, (source.ExpiresUtc - now).TotalSeconds);

        return new OperationalDiagnosticsCacheEntryMetadataDto
        {
            CacheKey = source.CacheKey,
            CacheKeyAlias = string.IsNullOrWhiteSpace(source.CacheKeyAlias)
                ? BuildKeyAlias(source.CacheKey)
                : source.CacheKeyAlias,
            KeyDomain = source.KeyDomain,
            Scope = source.Scope,
            Category = source.Category,
            CreatedUtc = source.CreatedUtc,
            ExpiresUtc = source.ExpiresUtc,
            LastServedUtc = source.LastServedUtc,
            StaleRisk = OperationalDiagnosticsCacheStaleRiskClassifier.Classify(
                source.CreatedUtc,
                source.ExpiresUtc,
                now),
            TtlSeconds = source.TtlSeconds,
            AgeSeconds = ageSeconds,
            RemainingTtlSeconds = remainingTtlSeconds
        };
    }

    private static OperationalDiagnosticsCacheEnvelope<T> Wrap<T>(
        T value,
        string cacheKey,
        string category,
        DateTime createdUtc,
        DateTime expiresUtc,
        DateTime servedUtc) where T : class =>
        new()
        {
            Value = value,
            CacheKey = cacheKey,
            Category = category,
            CreatedUtc = createdUtc,
            ExpiresUtc = expiresUtc,
            ServedUtc = servedUtc
        };

    internal static string BuildStorageKey(string category, string cacheKey) =>
        $"op-diag-cache:{category}:{cacheKey}";

    internal static string BuildKeyAlias(string cacheKey)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(cacheKey));
        return Convert.ToHexString(hash, 0, 6).ToLowerInvariant();
    }
}
