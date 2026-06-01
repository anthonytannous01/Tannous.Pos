namespace Tannous.Pos.Application.Audit;

/// <summary>Advisory survivability scoring (0–100; not persisted; non-authoritative).</summary>
public static class OperationalCacheSurvivabilityClassifier
{
    public static OperationalCacheSurvivabilityDto Compute(
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry,
        OperationalCacheGovernanceOverviewDto overview,
        OperationalCacheStabilityDto stability)
    {
        var total = telemetry.TotalHits + telemetry.TotalMisses;
        var staleServeRatio = total == 0
            ? 0d
            : (double)telemetry.TotalStaleServes / total;
        var bypassDenom = total + telemetry.TotalBypasses;
        var bypassRatio = bypassDenom == 0 ? 0d : (double)telemetry.TotalBypasses / bypassDenom;
        var saturation = overview.Cardinality.SaturationRatio;

        var score = 100;
        score -= (int)Math.Round(staleServeRatio * 30);
        score -= (int)Math.Min(20, telemetry.TotalInvalidations);
        score -= (int)Math.Round(saturation * 25);
        score -= overview.DegradationState switch
        {
            OperationalCacheDegradationState.SeverelyDegraded => 25,
            OperationalCacheDegradationState.Degraded => 15,
            OperationalCacheDegradationState.Recovering => 8,
            _ => 0
        };
        score -= overview.ReadinessState == OperationalCacheReadinessState.PressureDegraded ? 10 : 0;
        score -= (int)Math.Min(15, telemetry.RepeatedColdMisses);
        score -= (int)Math.Round(bypassRatio * 15);
        score = Math.Clamp(score, 0, 100);

        var classification = score switch
        {
            >= 80 => OperationalCacheSurvivabilityClassification.Durable,
            >= 60 => OperationalCacheSurvivabilityClassification.Stable,
            >= 40 => OperationalCacheSurvivabilityClassification.Fragile,
            _ => OperationalCacheSurvivabilityClassification.Volatile
        };

        var label = classification.ToString();
        var reasonCodes = OperationalCacheExplainabilityBuilder.Bound(new[]
        {
            $"Survivability{label}",
            staleServeRatio > 0.2 ? "ElevatedStaleServes" : string.Empty,
            saturation > 0.75 ? "HighSaturation" : string.Empty,
            telemetry.RepeatedColdMisses >= 2 ? "FrequentColdMisses" : string.Empty,
            bypassRatio > 0.25 ? "HighBypassRatio" : string.Empty
        });

        var action = classification switch
        {
            OperationalCacheSurvivabilityClassification.Durable =>
                "Cache survivability appears durable for current advisory window.",
            OperationalCacheSurvivabilityClassification.Stable =>
                "Monitor invalidation churn; survivability is stable but not guaranteed.",
            OperationalCacheSurvivabilityClassification.Fragile =>
                "Expect shorter effective TTL and higher stale-serve risk; reduce diagnostics pressure.",
            _ => "Treat cache as volatile; verify instance-local state and avoid authoritative decisions."
        };

        return new OperationalCacheSurvivabilityDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            SurvivabilityScore = score,
            Classification = classification,
            ClassificationLabel = label,
            RecommendedOperatorAction = action,
            ReasonCodes = reasonCodes,
            TriggerSignals = OperationalCacheExplainabilityBuilder.BuildStabilityTriggerSignals(stability),
            GovernanceNotes = OperationalCacheExplainabilityBuilder.Bound(new[]
            {
                OperationalCacheGovernanceFinalizationGovernance.GetAssumption(),
                $"Degradation:{overview.DegradationState}",
                $"Readiness:{overview.ReadinessState}"
            }),
            RecommendedActions = OperationalCacheExplainabilityBuilder.Bound(new[] { action })
        };
    }
}
