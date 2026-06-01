namespace Tannous.Pos.Application.Audit;

public static class OperationalCacheInvalidationSeverityClassifier
{
    public static OperationalCacheInvalidationSeverity Classify(
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry,
        int activeEntryCount,
        double scopeChurnRatio,
        int expiredEntryCount)
    {
        if (telemetry.InvalidationPressureEscalations > 0
            || telemetry.TotalInvalidations >= OperationalCacheInvalidationGovernance.CriticalInvalidationChurnThreshold)
            return OperationalCacheInvalidationSeverity.Critical;

        if (telemetry.CrossCategoryInvalidations >= 3
            || telemetry.TotalInvalidations >= OperationalCacheInvalidationGovernance.HighInvalidationChurnThreshold
            || scopeChurnRatio >= OperationalCacheInvalidationGovernance.HighScopeChurnRatio
            || expiredEntryCount >= 3)
            return OperationalCacheInvalidationSeverity.High;

        if (telemetry.TotalInvalidations >= 5
            || telemetry.ScopedInvalidations >= 3
            || scopeChurnRatio >= OperationalCacheInvalidationGovernance.ElevatedScopeChurnRatio
            || telemetry.CrossCategoryInvalidations > 0)
            return OperationalCacheInvalidationSeverity.Elevated;

        return OperationalCacheInvalidationSeverity.Informational;
    }
}
