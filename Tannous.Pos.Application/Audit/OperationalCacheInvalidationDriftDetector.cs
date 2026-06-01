namespace Tannous.Pos.Application.Audit;

/// <summary>Advisory invalidation drift detection (no throws; no auto-remediation).</summary>
public static class OperationalCacheInvalidationDriftDetector
{
    public static (OperationalCacheInvalidationDriftClassification Classification, IReadOnlyList<string> Signals)
        Detect(
            OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry,
            int activeEntryCount,
            int activeScopedKeyCount,
            int expiredEntryCount,
            double scopeChurnRatio)
    {
        var signals = new List<string>();

        if (telemetry.TotalInvalidations >= 10 && activeEntryCount == 0)
        {
            signals.Add("HighInvalidationsZeroActiveEntries");
        }

        if (telemetry.CrossCategoryInvalidations > 0 && telemetry.ScopedInvalidations == 0 && activeScopedKeyCount > 0)
        {
            signals.Add("CrossCategoryWithoutScopedInvalidations");
        }

        if (expiredEntryCount >= 2 && telemetry.FreshnessRecoveryCount == 0)
        {
            signals.Add("ExpiredEntriesWithoutRecovery");
        }

        if (scopeChurnRatio >= OperationalCacheInvalidationGovernance.HighScopeChurnRatio
            && telemetry.ScopedInvalidationRecoveries == 0)
        {
            signals.Add("ScopeChurnWithoutRecovery");
        }

        if (telemetry.InvalidationDriftCount > 0 && signals.Count == 0)
        {
            signals.Add("PriorDriftSignalsRecorded");
        }

        var classification = signals.Count switch
        {
            0 => OperationalCacheInvalidationDriftClassification.None,
            1 => OperationalCacheInvalidationDriftClassification.Minor,
            2 => OperationalCacheInvalidationDriftClassification.Moderate,
            _ => OperationalCacheInvalidationDriftClassification.Severe
        };

        return (classification, OperationalCacheInvalidationExplainabilityBuilder.Bound(signals));
    }
}
