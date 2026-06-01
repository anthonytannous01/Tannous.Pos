namespace Tannous.Pos.Application.Audit;

/// <summary>Operator-facing governance recommendations (advisory only).</summary>
public static class OperationalCacheGovernanceRecommendationBuilder
{
    public static IReadOnlyList<OperationalCacheGovernanceRecommendationDto> Build(
        OperationalCacheGovernanceOverviewDto overview,
        OperationalCacheGovernanceDriftDto drift,
        OperationalCacheSurvivabilityDto survivability)
    {
        var items = new List<OperationalCacheGovernanceRecommendationDto>();

        if (overview.PressureSeverity >= OperationalCachePressureSeverity.High)
        {
            items.Add(new OperationalCacheGovernanceRecommendationDto
            {
                Code = "ReduceDiagnosticsPressure",
                Summary = "Reduce broad query/export pressure before relying on cached diagnostics.",
                Priority = "High"
            });
        }

        if (drift.DriftDetected)
        {
            items.Add(new OperationalCacheGovernanceRecommendationDto
            {
                Code = "ReviewGovernanceDrift",
                Summary = "Review governance drift signals; classifications may be temporarily misaligned.",
                Priority = drift.DriftSeverity >= OperationalCacheGovernanceDriftSeverity.Moderate ? "High" : "Medium"
            });
        }

        if (survivability.Classification is OperationalCacheSurvivabilityClassification.Fragile
            or OperationalCacheSurvivabilityClassification.Volatile)
        {
            items.Add(new OperationalCacheGovernanceRecommendationDto
            {
                Code = "TreatCacheAdvisory",
                Summary = "Treat cache outputs as advisory only until survivability improves.",
                Priority = "High"
            });
        }

        if (overview.WarmRecommendationsSuppressed)
        {
            items.Add(new OperationalCacheGovernanceRecommendationDto
            {
                Code = "WarmRecommendationsSuppressed",
                Summary = "Warm-candidate recommendations suppressed under elevated cache pressure.",
                Priority = "Medium"
            });
        }

        if (items.Count == 0)
        {
            items.Add(new OperationalCacheGovernanceRecommendationDto
            {
                Code = "NoActionRequired",
                Summary = "No operator action required for current advisory governance window.",
                Priority = "Low"
            });
        }

        return items.Take(OperationalCacheGovernanceFinalizationGovernance.MaxExplainabilityItems).ToList();
    }
}
