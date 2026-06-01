namespace Tannous.Pos.Application.Audit;

public static class OperationalCacheContainmentRecommendationBuilder
{
    public static IReadOnlyList<OperationalCacheContainmentRecommendationDto> Build(
        OperationalCacheRecoveryContainmentState containment,
        OperationalCacheConsistencyConfidence confidence,
        OperationalCachePropagationSeverity propagation,
        OperationalCacheRecoveryWindowDto window)
    {
        var recommendations = new List<OperationalCacheContainmentRecommendationDto>();

        if (containment >= OperationalCacheRecoveryContainmentState.Escalated)
        {
            recommendations.Add(new OperationalCacheContainmentRecommendationDto
            {
                Code = "ContainDriftCascade",
                Priority = "High",
                Summary = "Compare propagation-diagnostics with invalidation-audit and governance-audit before trusting cached projections."
            });
        }

        if (propagation >= OperationalCachePropagationSeverity.Moderate)
        {
            recommendations.Add(new OperationalCacheContainmentRecommendationDto
            {
                Code = "InspectCrossCategoryExposure",
                Priority = "Medium",
                Summary = "Review category exposure counts; cross-category invalidation may require upstream cache reset between scenarios."
            });
        }

        if (confidence <= OperationalCacheConsistencyConfidence.Low)
        {
            recommendations.Add(new OperationalCacheContainmentRecommendationDto
            {
                Code = "ReduceOperatorTrust",
                Priority = "Medium",
                Summary = "Treat cache diagnostics as advisory until confidence stabilizes; prefer fresh upstream queries for critical decisions."
            });
        }

        if (window.ChurnReboundDetected)
        {
            recommendations.Add(new OperationalCacheContainmentRecommendationDto
            {
                Code = "WaitForStabilizationWindow",
                Priority = "Low",
                Summary = "Stale-risk rebound detected after recovery activity; re-query consistency-confidence after material changes settle."
            });
        }

        if (containment == OperationalCacheRecoveryContainmentState.Recovering)
        {
            recommendations.Add(new OperationalCacheContainmentRecommendationDto
            {
                Code = "MonitorRecoveryCycle",
                Priority = "Low",
                Summary = "Recovery cycle in progress; monitor consistency-recovery and containment-audit endpoints together."
            });
        }

        return recommendations
            .OrderBy(r => r.Priority, StringComparer.Ordinal)
            .ThenBy(r => r.Code, StringComparer.Ordinal)
            .Take(OperationalCacheConsistencyGovernance.MaxRecommendations)
            .ToList();
    }
}
