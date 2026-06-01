namespace Tannous.Pos.Application.Audit;

/// <summary>Shared threshold/ratio evaluation for governance classifiers (deterministic; no I/O).</summary>
public static class OperationalGovernanceThresholdEvaluator
{
    public static double ComputeHitRatio(long hits, long misses)
    {
        var total = hits + misses;
        return total == 0 ? 0d : (double)hits / total;
    }

    public static double ComputeBypassRatio(long hits, long misses, long bypasses)
    {
        var denominator = hits + misses + bypasses;
        return denominator == 0 ? 0d : (double)bypasses / denominator;
    }

    public static double ComputeStaleServeRatio(long staleServes, long hits, long misses)
    {
        var total = hits + misses;
        return total == 0 ? 0d : (double)staleServes / total;
    }

    public static double ComputeStaleServeRatioWithDefaultDenominator(long staleServes, long hits, long misses)
    {
        var denominator = hits + misses;
        if (denominator == 0)
            denominator = 1;

        return (double)staleServes / denominator;
    }

    public static int ClampScore(int score, int min = 0, int max = 100) =>
        Math.Clamp(score, min, max);

    public static bool IsAtOrAbove(double value, double threshold) =>
        value >= threshold;

    public static bool IsBelow(double value, double threshold) =>
        value < threshold;

    public static bool HasMinimumCount(long count, long minimum) =>
        count >= minimum;

    public static string ClassifyScoreBand(int score, int stableThreshold, int recoveringThreshold, int degradedThreshold)
    {
        if (score >= stableThreshold)
            return "Stable";
        if (score >= recoveringThreshold)
            return "Recovering";
        if (score >= degradedThreshold)
            return "Degraded";
        return "Unstable";
    }

    public static OperationalCacheConsistencyConfidence ClassifyConfidenceBand(int score)
    {
        if (score >= 75)
            return OperationalCacheConsistencyConfidence.High;
        if (score >= 50)
            return OperationalCacheConsistencyConfidence.Moderate;
        if (score >= 30)
            return OperationalCacheConsistencyConfidence.Low;
        return OperationalCacheConsistencyConfidence.Unstable;
    }

    public static string ClassifyConvergenceBand(int score)
    {
        if (score >= OperationalPressureGovernance.ConvergenceStableScoreThreshold)
            return "Stable";
        if (score >= 45)
            return "Moderate";
        if (score >= 25)
            return "Uncertain";
        return "Unstable";
    }
}
