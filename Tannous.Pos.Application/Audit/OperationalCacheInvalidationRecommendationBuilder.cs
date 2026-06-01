namespace Tannous.Pos.Application.Audit;

public static class OperationalCacheInvalidationRecommendationBuilder
{
    public static IReadOnlyList<OperationalCacheInvalidationRecommendationDto> Build(
        OperationalCacheInvalidationSeverity severity,
        OperationalCacheFreshnessRecoveryState recovery,
        OperationalCacheInvalidationDriftClassification drift)
    {
        var recommendations = new List<OperationalCacheInvalidationRecommendationDto>();

        if (severity >= OperationalCacheInvalidationSeverity.High)
        {
            recommendations.Add(new OperationalCacheInvalidationRecommendationDto
            {
                Code = "ReduceInvalidationChurn",
                Priority = "High",
                Summary = "Review reconciliation transitions and conflict recording frequency; prefer scoped invalidation where possible."
            });
        }

        if (recovery == OperationalCacheFreshnessRecoveryState.Unstable
            || recovery == OperationalCacheFreshnessRecoveryState.Churned)
        {
            recommendations.Add(new OperationalCacheInvalidationRecommendationDto
            {
                Code = "RequeryAfterMaterialChange",
                Priority = "Medium",
                Summary = "Re-query cache diagnostics after material operational changes; TTL-only expiry may still serve aging entries."
            });
        }

        if (drift >= OperationalCacheInvalidationDriftClassification.Moderate)
        {
            recommendations.Add(new OperationalCacheInvalidationRecommendationDto
            {
                Code = "ValidateInvalidationConsistency",
                Priority = "Medium",
                Summary = "Compare invalidation-consistency diagnostics with stale-risk and governance-audit projections."
            });
        }

        if (severity >= OperationalCacheInvalidationSeverity.Elevated)
        {
            recommendations.Add(new OperationalCacheInvalidationRecommendationDto
            {
                Code = "MonitorScopedKeys",
                Priority = "Low",
                Summary = "Inspect scoped invalidation diagnostics for device/operation scope churn."
            });
        }

        return recommendations
            .OrderBy(r => r.Priority, StringComparer.Ordinal)
            .ThenBy(r => r.Code, StringComparer.Ordinal)
            .Take(OperationalCacheInvalidationGovernance.MaxRecommendations)
            .ToList();
    }
}
