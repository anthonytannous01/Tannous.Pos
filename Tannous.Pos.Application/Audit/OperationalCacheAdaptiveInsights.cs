namespace Tannous.Pos.Application.Audit;

/// <summary>Advisory warm-candidate and readiness projections (visibility-only).</summary>
public static class OperationalCacheAdaptiveInsights
{
    private static readonly string[] TrackedCategories =
    {
        OperationalDiagnosticsCacheCategories.ResilienceMetrics,
        OperationalDiagnosticsCacheCategories.ReconciliationSummary,
        OperationalDiagnosticsCacheCategories.IncidentGroups,
        OperationalDiagnosticsCacheCategories.AlertSignals,
        OperationalDiagnosticsCacheCategories.AlertSummary
    };

    public static IReadOnlyList<OperationalCacheWarmCandidateDto> BuildWarmCandidates(
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry,
        OperationalCachePressureSeverity pressureSeverity = OperationalCachePressureSeverity.Nominal)
    {
        if (OperationalCachePressureClassifier.ShouldSuppressWarmRecommendations(pressureSeverity))
            return Array.Empty<OperationalCacheWarmCandidateDto>();

        var candidates = new List<OperationalCacheWarmCandidateDto>();

        foreach (var category in TrackedCategories)
        {
            if (!telemetry.ByCategory.TryGetValue(category, out var stats))
                continue;

            var repeatedCold = telemetry.RepeatedColdMissesByCategory.TryGetValue(category, out var cold)
                ? cold
                : 0;

            var isWarmCandidate = stats.Hits >= 2 && (stats.Misses >= 2 || repeatedCold >= 1);
            if (!isWarmCandidate)
                continue;

            candidates.Add(new OperationalCacheWarmCandidateDto
            {
                Category = category,
                HitCount = stats.Hits,
                MissCount = stats.Misses,
                RepeatedColdMissCount = repeatedCold,
                AdvisoryNote = "Advisory only; no automatic background warming is performed."
            });
        }

        return candidates
            .OrderByDescending(c => c.HitCount + c.MissCount)
            .ThenBy(c => c.Category, StringComparer.Ordinal)
            .ToList();
    }

    public static OperationalCacheReadinessState ClassifyReadiness(
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry,
        OperationalCacheAdaptivePressureSignals pressureSignals,
        int activeEntryCount,
        IReadOnlyList<OperationalCacheWarmCandidateDto> warmCandidates,
        OperationalCachePressureSeverity cachePressureSeverity = OperationalCachePressureSeverity.Nominal)
    {
        if (cachePressureSeverity is OperationalCachePressureSeverity.High or OperationalCachePressureSeverity.Critical
            || pressureSignals.ActiveSignalCount() > 0
            || telemetry.TotalBypasses > 0)
            return OperationalCacheReadinessState.PressureDegraded;

        if (warmCandidates.Count > 0)
            return OperationalCacheReadinessState.WarmingRecommended;

        if (activeEntryCount > 0 && telemetry.TotalHits > 0)
            return OperationalCacheReadinessState.Stable;

        return OperationalCacheReadinessState.Cold;
    }

    public static IReadOnlyList<string> GetWarmestCategories(
        IReadOnlyList<OperationalCacheWarmCandidateDto> candidates,
        int max = 5) =>
        candidates
            .OrderByDescending(c => c.HitCount)
            .Take(max)
            .Select(c => c.Category)
            .ToList();
}
