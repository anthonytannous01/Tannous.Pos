namespace Tannous.Pos.Application.Audit;

/// <summary>Advisory degradation detection (visibility only).</summary>
public static class OperationalCacheDegradationClassifier
{
    public static OperationalCacheDegradationDto Classify(
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry,
        OperationalCacheStabilityDto stability,
        OperationalCacheCardinalityClassification cardinality,
        OperationalCachePressureSeverity pressureSeverity)
    {
        var total = telemetry.TotalHits + telemetry.TotalMisses;
        var hitRatio = total == 0 ? 0d : (double)telemetry.TotalHits / total;
        var bypassDenom = total + telemetry.TotalBypasses;
        var bypassRatio = bypassDenom == 0 ? 0d : (double)telemetry.TotalBypasses / bypassDenom;

        var excessiveBypass = bypassRatio > 0.35 && telemetry.TotalBypasses >= 3;
        var unstableHitRatio = hitRatio < 0.45 && total >= 4;
        var saturatedScoped = cardinality is OperationalCacheCardinalityClassification.High
            or OperationalCacheCardinalityClassification.Saturated;
        var invalidationChurn = telemetry.TotalInvalidations >= 8;
        var persistentColdStart = telemetry.RepeatedColdMisses >= 3 && hitRatio < 0.5;

        var state = ClassifyState(
            stability,
            pressureSeverity,
            excessiveBypass,
            unstableHitRatio,
            saturatedScoped,
            invalidationChurn,
            persistentColdStart);

        var action = GetRecommendedAction(state);
        var reasonCodes = OperationalCacheExplainabilityBuilder.Bound(new[]
        {
            $"Degradation{state}",
            excessiveBypass ? "HighBypassRatio" : string.Empty,
            unstableHitRatio ? "LowHitRatio" : string.Empty,
            saturatedScoped ? "ScopedKeySaturation" : string.Empty,
            invalidationChurn ? "InvalidationChurn" : string.Empty,
            persistentColdStart ? "FrequentColdMisses" : string.Empty,
            pressureSeverity >= OperationalCachePressureSeverity.High ? "ElevatedPressure" : string.Empty
        });

        return new OperationalCacheDegradationDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            State = state,
            Classification = state.ToString(),
            RecommendedOperatorAction = action,
            ExcessiveBypassIndicated = excessiveBypass,
            UnstableHitRatioIndicated = unstableHitRatio,
            SaturatedScopedKeysIndicated = saturatedScoped,
            RepeatedInvalidationChurnIndicated = invalidationChurn,
            PersistentColdStartIndicated = persistentColdStart,
            ReasonCodes = reasonCodes,
            TriggerSignals = OperationalCacheExplainabilityBuilder.Bound(new[]
            {
                excessiveBypass ? "BypassPressure" : string.Empty,
                unstableHitRatio ? "HitRatioInstability" : string.Empty,
                saturatedScoped ? "CardinalitySaturation" : string.Empty
            }),
            GovernanceNotes = OperationalCacheExplainabilityBuilder.Bound(new[]
            {
                OperationalCacheGovernanceFinalizationGovernance.GetExplainabilityAssumption()
            }),
            RecommendedActions = OperationalCacheExplainabilityBuilder.Bound(new[] { action })
        };
    }

    private static OperationalCacheDegradationState ClassifyState(
        OperationalCacheStabilityDto stability,
        OperationalCachePressureSeverity pressureSeverity,
        bool excessiveBypass,
        bool unstableHitRatio,
        bool saturatedScoped,
        bool invalidationChurn,
        bool persistentColdStart)
    {
        if (pressureSeverity == OperationalCachePressureSeverity.Critical
            || stability.StabilityClassification == "Unstable"
            || (saturatedScoped && excessiveBypass))
            return OperationalCacheDegradationState.SeverelyDegraded;

        if (pressureSeverity == OperationalCachePressureSeverity.High
            || stability.StabilityClassification == "Degraded"
            || excessiveBypass
            || unstableHitRatio
            || invalidationChurn)
            return OperationalCacheDegradationState.Degraded;

        if (stability.StabilityClassification == "Recovering"
            || persistentColdStart)
            return OperationalCacheDegradationState.Recovering;

        return OperationalCacheDegradationState.Healthy;
    }

    private static string GetRecommendedAction(OperationalCacheDegradationState state) =>
        state switch
        {
            OperationalCacheDegradationState.Healthy =>
                "Cache governance within expected advisory bounds.",
            OperationalCacheDegradationState.Recovering =>
                "Cache recovering; continue monitoring hit ratio and invalidation churn.",
            OperationalCacheDegradationState.Degraded =>
                "Reduce diagnostics pressure; expect eventual freshness delays.",
            OperationalCacheDegradationState.SeverelyDegraded =>
                "Do not treat cache as authoritative; investigate bypass and cardinality saturation.",
            _ => "Monitor cache governance overview."
        };
}
