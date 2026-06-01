namespace Tannous.Pos.Application.Audit;

public static class OperationalCacheFreshnessRecoveryClassifier
{
    public static OperationalCacheFreshnessRecoveryState Classify(
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry,
        int agingEntryCount,
        int nearExpiryEntryCount,
        int expiredEntryCount,
        int activeEntryCount)
    {
        if (activeEntryCount == 0 && telemetry.TotalInvalidations == 0)
            return OperationalCacheFreshnessRecoveryState.Stable;

        if (expiredEntryCount >= 2 && telemetry.FreshnessRecoveryCount == 0)
            return OperationalCacheFreshnessRecoveryState.Unstable;

        if (telemetry.TotalInvalidations >= OperationalCacheInvalidationGovernance.HighInvalidationChurnThreshold
            && (agingEntryCount + nearExpiryEntryCount) >= 2)
            return OperationalCacheFreshnessRecoveryState.Churned;

        if (telemetry.FreshnessRecoveryCount > 0
            || (telemetry.ScopedInvalidationRecoveries > 0 && expiredEntryCount == 0))
            return OperationalCacheFreshnessRecoveryState.Recovering;

        if (expiredEntryCount == 0 && agingEntryCount <= 1 && nearExpiryEntryCount <= 1)
            return OperationalCacheFreshnessRecoveryState.Stable;

        return telemetry.TotalInvalidations >= 5
            ? OperationalCacheFreshnessRecoveryState.Churned
            : OperationalCacheFreshnessRecoveryState.Recovering;
    }
}
