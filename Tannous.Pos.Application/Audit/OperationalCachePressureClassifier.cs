namespace Tannous.Pos.Application.Audit;

/// <summary>
/// Heuristic cache pressure severity (entry/telemetry only; no OS memory APIs).
/// GOVERNANCE / NON-GOAL: no GC.GetTotalMemory; not authoritative.
/// </summary>
public static class OperationalCachePressureClassifier
{
    public static OperationalCachePressureSeverity Classify(
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry,
        IReadOnlyList<OperationalDiagnosticsCacheEntryMetadataDto> entries,
        OperationalCacheCardinalityClassification cardinality)
    {
        var active = entries.Count;
        var max = OperationalDiagnosticsCacheConstants.MaxCacheEntryCount;
        var saturation = max == 0 ? 0d : (double)active / max;
        var total = telemetry.TotalHits + telemetry.TotalMisses;
        var hitRatio = total == 0 ? 1d : (double)telemetry.TotalHits / total;
        var bypassRatio = total + telemetry.TotalBypasses == 0
            ? 0d
            : (double)telemetry.TotalBypasses / (total + telemetry.TotalBypasses);

        if (cardinality == OperationalCacheCardinalityClassification.Saturated
            || saturation >= OperationalCacheCardinalityGovernance.SaturatedActiveEntryRatio
            || (telemetry.RepeatedColdMisses >= 5 && hitRatio < 0.35)
            || (telemetry.TotalInvalidations >= 20 && bypassRatio > 0.4))
            return OperationalCachePressureSeverity.Critical;

        if (cardinality == OperationalCacheCardinalityClassification.High
            || telemetry.RepeatedColdMisses >= 3
            || telemetry.TotalInvalidations >= 10
            || bypassRatio > 0.3
            || hitRatio < 0.4)
            return OperationalCachePressureSeverity.High;

        if (cardinality == OperationalCacheCardinalityClassification.Elevated
            || telemetry.TotalBypasses > 0
            || telemetry.RepeatedColdMisses >= 1
            || telemetry.TotalInvalidations >= 3)
            return OperationalCachePressureSeverity.Elevated;

        return OperationalCachePressureSeverity.Nominal;
    }

    public static string GetRecommendedAction(OperationalCachePressureSeverity severity) =>
        severity switch
        {
            OperationalCachePressureSeverity.Nominal =>
                "No cache pressure action required.",
            OperationalCachePressureSeverity.Elevated =>
                "Monitor cache effectiveness; reduce broad diagnostics queries if misses increase.",
            OperationalCachePressureSeverity.High =>
                "Expect shorter TTL windows and degraded readiness; avoid repeated full exports.",
            OperationalCachePressureSeverity.Critical =>
                "Treat cache as advisory only; reduce query/export pressure and verify instance-local state.",
            _ => "Monitor cache diagnostics."
        };

    public static bool ShouldSuppressWarmRecommendations(OperationalCachePressureSeverity severity) =>
        severity is OperationalCachePressureSeverity.High or OperationalCachePressureSeverity.Critical;
}
