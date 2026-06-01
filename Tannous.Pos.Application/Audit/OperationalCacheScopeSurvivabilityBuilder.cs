namespace Tannous.Pos.Application.Audit;

/// <summary>Scoped-key survivability projections (sanitized aliases only).</summary>
public static class OperationalCacheScopeSurvivabilityBuilder
{
    public static OperationalCacheScopeDiagnosticsDto Build(
        IReadOnlyList<OperationalDiagnosticsCacheEntryMetadataDto> entries,
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry)
    {
        var scoped = entries
            .Where(e => !string.Equals(e.Scope, OperationalDiagnosticsCacheScopes.Global, StringComparison.Ordinal))
            .ToList();

        var scopedCount = scoped.Count;
        var invalidationChurn = telemetry.TotalInvalidations;
        var scopedInvalidations = telemetry.ScopedInvalidations;
        var churnRatio = invalidationChurn == 0
            ? 0d
            : Math.Round((double)scopedInvalidations / invalidationChurn, 4);

        var oldest = scoped
            .OrderByDescending(e => e.AgeSeconds)
            .Take(10)
            .Select(e => new OperationalCacheScopedEntrySurvivabilityDto
            {
                Category = e.Category,
                Scope = e.Scope,
                CacheKeyAlias = e.CacheKeyAlias,
                AgeSeconds = e.AgeSeconds,
                RemainingTtlSeconds = e.RemainingTtlSeconds
            })
            .ToList();

        var scopeByCategory = scoped
            .GroupBy(e => e.Category, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        return new OperationalCacheScopeDiagnosticsDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            ActiveScopedKeyCount = scopedCount,
            TotalInvalidations = invalidationChurn,
            InvalidationsByScopedKeys = scopedInvalidations,
            ScopeChurnRatio = churnRatio,
            ScopeDistributionByCategory = scopeByCategory,
            OldestScopedEntries = oldest,
            GovernanceNote =
                "Scope survivability uses sanitized aliases only; no payload or raw identifier exposure."
        };
    }
}
