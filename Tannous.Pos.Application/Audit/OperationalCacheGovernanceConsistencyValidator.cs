namespace Tannous.Pos.Application.Audit;

/// <summary>Lightweight advisory consistency checks (never throws).</summary>
public static class OperationalCacheGovernanceConsistencyValidator
{
    public static OperationalCacheGovernanceConsistencyDto Validate(
        OperationalCacheGovernanceOverviewDto overview,
        OperationalCacheSurvivabilityDto survivability)
    {
        var notes = new List<string>();
        var inconsistencies = new List<string>();
        var reasons = new List<string>();

        if (!IsAdaptiveTtlAligned(overview))
        {
            inconsistencies.Add("AdaptiveTtlPressureMismatch");
            reasons.Add("TtlModePressureIncoherent");
        }
        else
        {
            notes.Add("AdaptiveTtlAlignedWithPressure");
        }

        if (!IsReadinessDegradationCoherent(overview))
        {
            inconsistencies.Add("ReadinessDegradationMismatch");
            reasons.Add("ReadinessDegradationIncoherent");
        }
        else
        {
            notes.Add("ReadinessDegradationCoherent");
        }

        if (!IsSurvivabilityStaleRiskAligned(overview, survivability))
        {
            inconsistencies.Add("SurvivabilityStaleRiskMismatch");
            reasons.Add("SurvivabilityStaleIncoherent");
        }
        else
        {
            notes.Add("SurvivabilityAlignedWithStaleRisk");
        }

        if (!IsOverviewCoherent(overview))
        {
            inconsistencies.Add("OverviewAggregateMismatch");
            reasons.Add("OverviewIncoherent");
        }
        else
        {
            notes.Add("OverviewAggregatesCoherent");
        }

        var isConsistent = inconsistencies.Count == 0;

        return new OperationalCacheGovernanceConsistencyDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            IsConsistent = isConsistent,
            ConsistencyNotes = OperationalCacheExplainabilityBuilder.Bound(notes),
            InconsistencySignals = OperationalCacheExplainabilityBuilder.Bound(inconsistencies),
            ReasonCodes = OperationalCacheExplainabilityBuilder.Bound(reasons),
            GovernanceNotes = OperationalCacheExplainabilityBuilder.Bound(new[]
            {
                isConsistent
                    ? "Governance projections are internally consistent."
                    : "Inconsistencies are advisory; operators should cross-check diagnostics endpoints.",
                OperationalCacheGovernanceFinalizationGovernance.GetAssumption()
            })
        };
    }

    private static bool IsAdaptiveTtlAligned(OperationalCacheGovernanceOverviewDto overview)
    {
        if (overview.PressureSeverity == OperationalCachePressureSeverity.Nominal)
            return overview.DominantTtlMode == OperationalCacheTtlMode.Normal
                || overview.DominantTtlMode == OperationalCacheTtlMode.Reduced;

        if (overview.PressureSeverity >= OperationalCachePressureSeverity.High)
            return overview.DominantTtlMode is OperationalCacheTtlMode.Minimal
                or OperationalCacheTtlMode.BypassPreferred;

        return true;
    }

    private static bool IsReadinessDegradationCoherent(OperationalCacheGovernanceOverviewDto overview)
    {
        if (overview.ReadinessState == OperationalCacheReadinessState.PressureDegraded)
            return overview.DegradationState != OperationalCacheDegradationState.Healthy
                || overview.PressureSeverity >= OperationalCachePressureSeverity.Elevated;

        if (overview.DegradationState == OperationalCacheDegradationState.SeverelyDegraded)
            return overview.ReadinessState == OperationalCacheReadinessState.PressureDegraded
                || overview.PressureSeverity >= OperationalCachePressureSeverity.High;

        return true;
    }

    private static bool IsSurvivabilityStaleRiskAligned(
        OperationalCacheGovernanceOverviewDto overview,
        OperationalCacheSurvivabilityDto survivability)
    {
        var atRisk = overview.AgingEntryCount + overview.NearExpiryEntryCount + overview.ExpiredEntryCount;
        if (atRisk >= 5 && survivability.Classification == OperationalCacheSurvivabilityClassification.Durable)
            return false;

        if (atRisk == 0 && survivability.Classification == OperationalCacheSurvivabilityClassification.Volatile)
            return false;

        return true;
    }

    private static bool IsOverviewCoherent(OperationalCacheGovernanceOverviewDto overview) =>
        overview.Cardinality.Classification == overview.CardinalityClassification
        && overview.ActiveScopedKeyCount == overview.Cardinality.ActiveScopedKeyCount;
}
