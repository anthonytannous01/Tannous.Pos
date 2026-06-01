namespace Tannous.Pos.Application.Audit;

/// <summary>Deterministic cardinality classification from active entry metadata.</summary>
public static class OperationalCacheCardinalityClassifier
{
    public static OperationalCacheCardinalitySnapshotDto BuildSnapshot(
        IReadOnlyList<OperationalDiagnosticsCacheEntryMetadataDto> entries)
    {
        var max = OperationalDiagnosticsCacheConstants.MaxCacheEntryCount;
        var active = entries.Count;
        var scoped = entries.Count(e =>
            !string.Equals(e.Scope, OperationalDiagnosticsCacheScopes.Global, StringComparison.Ordinal));
        var global = active - scoped;
        var saturation = max == 0 ? 0d : (double)active / max;
        var scopedRatio = active == 0 ? 0d : (double)scoped / active;

        var classification = Classify(active, scoped, saturation, scopedRatio);

        return new OperationalCacheCardinalitySnapshotDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            Classification = classification,
            ActiveEntryCount = active,
            MaxCacheEntryCount = max,
            ActiveScopedKeyCount = scoped,
            GlobalEntryCount = global,
            SaturationRatio = Math.Round(saturation, 4),
            ScopedEntryRatio = Math.Round(scopedRatio, 4),
            EntriesByScope = entries
                .GroupBy(e => e.Scope, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal),
            EntriesByCategory = entries
                .GroupBy(e => e.Category, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal),
            GovernanceNote = OperationalCacheCardinalityGovernance.GetAssumption()
        };
    }

    public static OperationalCacheCardinalityClassification Classify(
        int activeEntryCount,
        int activeScopedKeyCount,
        double saturationRatio,
        double scopedEntryRatio)
    {
        if (activeEntryCount >= OperationalDiagnosticsCacheConstants.MaxCacheEntryCount
            || saturationRatio >= OperationalCacheCardinalityGovernance.SaturatedActiveEntryRatio
            || (scopedEntryRatio >= 0.6 && activeScopedKeyCount >= OperationalCacheCardinalityGovernance.HighScopedKeyThreshold))
            return OperationalCacheCardinalityClassification.Saturated;

        if (saturationRatio >= OperationalCacheCardinalityGovernance.HighActiveEntryRatio
            || activeScopedKeyCount >= OperationalCacheCardinalityGovernance.HighScopedKeyThreshold)
            return OperationalCacheCardinalityClassification.High;

        if (saturationRatio >= OperationalCacheCardinalityGovernance.ElevatedActiveEntryRatio
            || activeScopedKeyCount >= OperationalCacheCardinalityGovernance.ElevatedScopedKeyThreshold)
            return OperationalCacheCardinalityClassification.Elevated;

        return OperationalCacheCardinalityClassification.Normal;
    }
}
