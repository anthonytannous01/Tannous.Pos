namespace Tannous.Pos.Application.Audit;

public static class OperationalCacheRecoveryWindowClassifier
{
    public static OperationalCacheRecoveryWindowDto Classify(
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry,
        OperationalDiagnosticsCacheDiagnosticsStaleRiskDto staleRisk,
        int activeEntryCount,
        double hitRatio)
    {
        var churnRebound = telemetry.TotalInvalidations >= OperationalCacheConsistencyGovernance.StabilizationChurnReboundThreshold
                           && staleRisk.ExpiredEntryCount >= 1
                           && telemetry.RecoveryWindowExtensions > 0;

        var stabilizationAchieved = activeEntryCount >= 0
                                    && staleRisk.ExpiredEntryCount == 0
                                    && hitRatio >= OperationalCacheConsistencyGovernance.LowHitRatioThreshold
                                    && telemetry.RecoveryWindowExtensions == 0;

        string classification;
        if (stabilizationAchieved && telemetry.ConsistencyRecoveryCycles > 0)
            classification = "Stabilized";
        else if (churnRebound)
            classification = "ChurnRebound";
        else if (telemetry.RecoveryWindowExtensions > 0)
            classification = "Extended";
        else if (telemetry.ConsistencyRecoveryCycles > 0 || telemetry.FreshnessRecoveryCount > 0)
            classification = "Recovering";
        else
            classification = "Nominal";

        var stabilizationSignals = OperationalCacheConsistencyExplainabilityBuilder.Bound(new[]
        {
            stabilizationAchieved ? "StabilizationAchieved" : string.Empty,
            churnRebound ? "ChurnReboundDetected" : string.Empty,
            telemetry.RecoveryWindowExtensions > 0 ? "RecoveryWindowExtended" : string.Empty,
            staleRisk.NearExpiryEntryCount > 0 ? "NearExpiryPresent" : string.Empty
        });

        return new OperationalCacheRecoveryWindowDto
        {
            WindowClassification = classification,
            StabilizationAchieved = stabilizationAchieved,
            ChurnReboundDetected = churnRebound,
            RecoveryWindowExtensions = telemetry.RecoveryWindowExtensions,
            ConsistencyRecoveryCycles = telemetry.ConsistencyRecoveryCycles,
            ExpiredEntryCount = staleRisk.ExpiredEntryCount,
            StabilizationSignals = stabilizationSignals
        };
    }
}
