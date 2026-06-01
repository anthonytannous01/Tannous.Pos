namespace Tannous.Pos.Application.Audit.Governance;

public static class OperationalGovernanceProjectionReuseClassifier
{
    public static OperationalGovernanceProjectionReuseLevel Classify(
        long projectionReuseHits,
        long projectionReuseMisses)
    {
        var total = projectionReuseHits + projectionReuseMisses;
        if (total <= 0)
            return OperationalGovernanceProjectionReuseLevel.None;

        var ratio = (double)projectionReuseHits / total;
        if (ratio >= 0.75)
            return OperationalGovernanceProjectionReuseLevel.Dominant;
        if (ratio >= 0.4)
            return OperationalGovernanceProjectionReuseLevel.Significant;
        if (projectionReuseHits > 0)
            return OperationalGovernanceProjectionReuseLevel.Partial;

        return OperationalGovernanceProjectionReuseLevel.None;
    }

    public static double ComputeHitRatio(long hits, long misses)
    {
        var total = hits + misses;
        return total == 0 ? 0 : Math.Round((double)hits / total, 4);
    }
}
