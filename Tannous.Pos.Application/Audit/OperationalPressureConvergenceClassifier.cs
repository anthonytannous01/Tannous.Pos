namespace Tannous.Pos.Application.Audit;

public static class OperationalPressureConvergenceClassifier
{
    public static (string Classification, int Score) Classify(
        IOperationalResiliencePressureState pressureState,
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry,
        OperationalPressureLifecycleSnapshot lifecycle,
        OperationalCacheGovernanceOverviewDto overview,
        OperationalCacheStabilityDto stability)
    {
        var score = 100;

        if (pressureState.QueryDateRangeClamped)
            score -= 20;
        if (pressureState.QueryPageSizeClamped)
            score -= 15;
        if (pressureState.ForensicExportTruncated)
            score -= 15;
        if (lifecycle.StickyPressureDetected)
            score -= 25;
        if (overview.PressureSeverity >= OperationalCachePressureSeverity.High)
            score -= 20;
        if (overview.ReadinessState == OperationalCacheReadinessState.PressureDegraded)
            score -= 15;
        if (telemetry.ConsistencyConfidenceDrops > 0)
            score -= 10;
        if (stability.StabilityScore < 50)
            score -= 10;

        if (!pressureState.QueryDateRangeClamped
            && !pressureState.QueryPageSizeClamped
            && !pressureState.ForensicExportTruncated
            && telemetry.PressureRecoveryCycles > 0)
            score += 5;

        score = OperationalGovernanceThresholdEvaluator.ClampScore(score);

        var classification = OperationalGovernanceClassificationNormalizer.NormalizeConvergenceClassification(
            OperationalGovernanceThresholdEvaluator.ClassifyConvergenceBand(score));

        return (classification, score);
    }
}
