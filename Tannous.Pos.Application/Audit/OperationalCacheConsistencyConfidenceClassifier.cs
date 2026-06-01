namespace Tannous.Pos.Application.Audit;

public static class OperationalCacheConsistencyConfidenceClassifier
{
    public static (OperationalCacheConsistencyConfidence Level, int Score) Classify(
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry,
        OperationalCacheStabilityDto stability,
        OperationalCacheGovernanceOverviewDto overview,
        int expiredEntryCount)
    {
        var score = 100;

        var hitRatio = OperationalGovernanceThresholdEvaluator.ComputeHitRatio(
            telemetry.TotalHits,
            telemetry.TotalMisses);
        var bypassRatio = OperationalGovernanceThresholdEvaluator.ComputeBypassRatio(
            telemetry.TotalHits,
            telemetry.TotalMisses,
            telemetry.TotalBypasses);
        var staleRatio = OperationalGovernanceThresholdEvaluator.ComputeStaleServeRatioWithDefaultDenominator(
            telemetry.TotalStaleServes,
            telemetry.TotalHits,
            telemetry.TotalMisses);

        if (hitRatio < OperationalCacheConsistencyGovernance.LowHitRatioThreshold)
            score -= 25;
        if (bypassRatio >= OperationalCacheConsistencyGovernance.ElevatedBypassRatioThreshold)
            score -= 20;
        if (staleRatio > 0.15)
            score -= 15;
        if (expiredEntryCount >= 2)
            score -= 20;
        if (telemetry.ConsistencyConfidenceDrops > 0)
            score -= 10;
        if (telemetry.PropagationDetections >= 2)
            score -= 15;
        if (overview.PressureSeverity >= OperationalCachePressureSeverity.High)
            score -= 15;
        if (overview.DegradationState is OperationalCacheDegradationState.Degraded
            or OperationalCacheDegradationState.SeverelyDegraded)
            score -= 10;

        score = OperationalGovernanceThresholdEvaluator.ClampScore(score);

        var level = OperationalGovernanceClassificationNormalizer.NormalizeConfidence(
            OperationalGovernanceThresholdEvaluator.ClassifyConfidenceBand(score));

        return (level, score);
    }
}
