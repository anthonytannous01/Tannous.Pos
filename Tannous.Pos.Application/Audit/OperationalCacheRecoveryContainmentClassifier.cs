namespace Tannous.Pos.Application.Audit;

public static class OperationalCacheRecoveryContainmentClassifier
{
    public static OperationalCacheRecoveryContainmentState Classify(
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry,
        OperationalCacheGovernanceOverviewDto overview,
        OperationalCachePropagationSeverity propagation,
        int expiredEntryCount,
        int activeEntryCount)
    {
        if (telemetry.ContainmentEscalations > 0
            || propagation == OperationalCachePropagationSeverity.Severe
            || (overview.PressureSeverity == OperationalCachePressureSeverity.Critical && expiredEntryCount >= 2))
            return OperationalCacheRecoveryContainmentState.Escalated;

        if (propagation >= OperationalCachePropagationSeverity.Moderate
            || telemetry.CrossCategoryInvalidations >= 2
            || telemetry.RecoveryWindowExtensions >= 2)
            return OperationalCacheRecoveryContainmentState.Contained;

        if (telemetry.ConsistencyRecoveryCycles > 0
            || telemetry.FreshnessRecoveryCount > 0
            || overview.ReadinessState == OperationalCacheReadinessState.WarmingRecommended
            || (activeEntryCount > 0 && expiredEntryCount == 0 && telemetry.TotalInvalidations > 0))
            return OperationalCacheRecoveryContainmentState.Recovering;

        return OperationalCacheRecoveryContainmentState.Stable;
    }
}
