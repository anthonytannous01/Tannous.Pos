namespace Tannous.Pos.Application.Audit;

public static class OperationalCachePropagationDetector
{
    private static readonly string[] PropagationCategories =
    [
        OperationalDiagnosticsCacheCategories.ReconciliationSummary,
        OperationalDiagnosticsCacheCategories.IncidentGroups,
        OperationalDiagnosticsCacheCategories.AlertSignals,
        OperationalDiagnosticsCacheCategories.AlertSummary,
        OperationalDiagnosticsCacheCategories.ResilienceMetrics
    ];

    public static (OperationalCachePropagationSeverity Severity, IReadOnlyList<string> Signals, IReadOnlyDictionary<string, int> Exposure)
        Detect(
            IReadOnlyList<OperationalDiagnosticsCacheEntryMetadataDto> entries,
            OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry)
    {
        var exposure = entries
            .GroupBy(e => e.Category, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        var signals = new List<string>();
        var exposedPropagationCategories = PropagationCategories
            .Count(c => exposure.ContainsKey(c));

        if (telemetry.CrossCategoryInvalidations > 0)
            signals.Add("CrossCategoryInvalidationObserved");

        if (telemetry.PropagationDetections > 0)
            signals.Add("PropagationTelemetryRecorded");

        if (exposedPropagationCategories >= 3)
            signals.Add("MultiCategoryExposure");

        if (telemetry.InvalidationDriftCount > 0 && telemetry.CrossCategoryInvalidations > 0)
            signals.Add("DriftWithCrossCategory");

        var staleCategories = entries
            .Where(e => e.StaleRisk != OperationalDiagnosticsCacheStaleRisk.Fresh)
            .Select(e => e.Category)
            .Distinct(StringComparer.Ordinal)
            .Count();
        if (staleCategories >= 2)
            signals.Add("StaleRiskAcrossCategories");

        var severity = signals.Count switch
        {
            0 => OperationalCachePropagationSeverity.None,
            1 => OperationalCachePropagationSeverity.Minor,
            2 => OperationalCachePropagationSeverity.Moderate,
            _ => OperationalCachePropagationSeverity.Severe
        };

        if (telemetry.CrossCategoryInvalidations >= 2 && severity < OperationalCachePropagationSeverity.Moderate)
            severity = OperationalCachePropagationSeverity.Moderate;

        return (
            severity,
            OperationalCacheConsistencyExplainabilityBuilder.Bound(signals),
            exposure);
    }
}
