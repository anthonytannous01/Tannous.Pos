namespace Tannous.Pos.Application.Audit;

/// <summary>Lightweight in-process stability scoring (0–100; not persisted).</summary>
public static class OperationalCacheStabilityClassifier
{
    public static OperationalCacheStabilityDto Compute(OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry)
    {
        var totalRequests = telemetry.TotalHits + telemetry.TotalMisses;
        var hitRatio = OperationalGovernanceThresholdEvaluator.ComputeHitRatio(
            telemetry.TotalHits,
            telemetry.TotalMisses);
        var staleServeRatio = OperationalGovernanceThresholdEvaluator.ComputeStaleServeRatio(
            telemetry.TotalStaleServes,
            telemetry.TotalHits,
            telemetry.TotalMisses);
        var bypassRatio = OperationalGovernanceThresholdEvaluator.ComputeBypassRatio(
            telemetry.TotalHits,
            telemetry.TotalMisses,
            telemetry.TotalBypasses);

        var score = 100;
        score -= (int)Math.Round((1d - hitRatio) * 35);
        score -= (int)Math.Round(staleServeRatio * 25);
        score -= (int)Math.Round(bypassRatio * 20);
        score -= (int)Math.Min(15, telemetry.TotalInvalidations);
        score -= (int)Math.Min(15, telemetry.RepeatedColdMisses);

        score = OperationalGovernanceThresholdEvaluator.ClampScore(score);

        var classification = OperationalGovernanceClassificationNormalizer.NormalizeStabilityClassification(
            OperationalGovernanceThresholdEvaluator.ClassifyScoreBand(score, 80, 60, 40));

        var action = classification switch
        {
            "Stable" => "No action required; cache operating within expected bounds.",
            "Recovering" => "Monitor pressure indicators; consider query scope reduction if misses persist.",
            "Degraded" => "Review operational pressure and invalidation churn; expect eventual freshness delays.",
            _ => "Treat diagnostics as advisory only; reduce export/query pressure and verify instance-local cache state."
        };

        return new OperationalCacheStabilityDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            StabilityScore = score,
            StabilityClassification = classification,
            RecommendedOperatorAction = action,
            HitRatio = Math.Round(hitRatio, 4),
            StaleServeRatio = Math.Round(staleServeRatio, 4),
            BypassRatio = Math.Round(bypassRatio, 4),
            InvalidationChurn = telemetry.TotalInvalidations,
            RepeatedColdMisses = telemetry.RepeatedColdMisses
        };
    }
}
