namespace Tannous.Pos.Application.Audit;

/// <summary>Builds computed governance audit projections (no persistence).</summary>
public static class OperationalCacheGovernanceAuditBuilder
{
    public static OperationalCacheGovernanceAuditDto Build(
        IReadOnlyList<OperationalDiagnosticsCacheEntryMetadataDto> entries,
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry,
        OperationalCacheAdaptivePressureSignals pressureSignals)
    {
        var overview = OperationalCacheGovernanceProjectionBuilder.BuildOverview(
            entries,
            telemetry,
            pressureSignals);
        var stability = EnrichStability(OperationalCacheStabilityClassifier.Compute(telemetry), overview, telemetry);
        var survivability = OperationalCacheSurvivabilityClassifier.Compute(telemetry, overview, stability);
        var drift = OperationalCacheGovernanceDriftDetector.Detect(overview, telemetry, stability);
        var consistency = OperationalCacheGovernanceConsistencyValidator.Validate(overview, survivability);
        var recommendations = OperationalCacheGovernanceRecommendationBuilder.Build(overview, drift, survivability);

        var pressureAligned = overview.PressureSeverity switch
        {
            OperationalCachePressureSeverity.Critical or OperationalCachePressureSeverity.High =>
                overview.DegradationState is OperationalCacheDegradationState.Degraded
                    or OperationalCacheDegradationState.SeverelyDegraded,
            _ => true
        };

        var ttlAligned = consistency.InconsistencySignals.Contains("AdaptiveTtlPressureMismatch", StringComparer.Ordinal) == false;
        var invalidationHealthy = telemetry.TotalInvalidations < 25 || overview.ActiveEntryCount > 0;
        var readinessCoherent = consistency.InconsistencySignals.Contains("ReadinessDegradationMismatch", StringComparer.Ordinal) == false;
        var scopedAligned = consistency.InconsistencySignals.Contains("CardinalityInvalidationMismatch", StringComparer.Ordinal) == false;

        var reasonCodes = OperationalCacheExplainabilityBuilder.Bound(
            overview.Degradation.ReasonCodes
                .Concat(stability.ReasonCodes)
                .Concat(OperationalCacheExplainabilityBuilder.BuildPressureReasonCodes(
                    overview.PressureSeverity,
                    telemetry,
                    overview.CardinalityClassification)));

        var triggerSignals = OperationalCacheExplainabilityBuilder.Bound(
            stability.TriggerSignals.Concat(drift.DriftSignals));

        var actions = OperationalCacheExplainabilityBuilder.Bound(
            recommendations.Select(r => r.Summary)
                .Concat(new[] { stability.RecommendedOperatorAction, survivability.RecommendedOperatorAction }));

        return new OperationalCacheGovernanceAuditDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            CacheModeConsistent = overview.ActiveEntryCount <= overview.Cardinality.MaxCacheEntryCount,
            AdaptiveTtlAligned = ttlAligned,
            PressureDegradationAligned = pressureAligned,
            InvalidationSurvivabilityHealthy = invalidationHealthy,
            ReadinessDegradationCoherent = readinessCoherent,
            ScopedKeySaturationAligned = scopedAligned,
            DominantTtlMode = overview.DominantTtlMode.ToString(),
            PressureSeverity = overview.PressureSeverity.ToString(),
            DegradationState = overview.DegradationState.ToString(),
            ReadinessState = overview.ReadinessState.ToString(),
            CardinalityClassification = overview.CardinalityClassification.ToString(),
            StabilityScore = stability.StabilityScore,
            SurvivabilityScore = survivability.SurvivabilityScore,
            SurvivabilityClassification = survivability.ClassificationLabel,
            AgingEntryCount = overview.AgingEntryCount,
            NearExpiryEntryCount = overview.NearExpiryEntryCount,
            ExpiredEntryCount = overview.ExpiredEntryCount,
            Drift = drift,
            Consistency = consistency,
            Recommendations = recommendations,
            ReasonCodes = reasonCodes,
            TriggerSignals = triggerSignals,
            GovernanceNotes = OperationalCacheExplainabilityBuilder.Bound(new[]
            {
                OperationalCacheGovernanceFinalizationGovernance.GetAssumption(),
                OperationalCacheGovernanceFinalizationGovernance.GetExplainabilityAssumption()
            }),
            RecommendedActions = actions
        };
    }

    public static OperationalCacheStabilityDto EnrichStability(
        OperationalCacheStabilityDto stability,
        OperationalCacheGovernanceOverviewDto overview,
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry)
    {
        var reasonCodes = OperationalCacheExplainabilityBuilder.Bound(new[]
        {
            $"Stability{stability.StabilityClassification}",
            stability.BypassRatio > 0.25 ? "HighBypassRatio" : string.Empty,
            stability.StaleServeRatio > 0.15 ? "ElevatedStaleServes" : string.Empty,
            stability.RepeatedColdMisses >= 2 ? "FrequentColdMisses" : string.Empty
        });

        return new OperationalCacheStabilityDto
        {
            GeneratedAtUtc = stability.GeneratedAtUtc,
            StabilityScore = stability.StabilityScore,
            StabilityClassification = stability.StabilityClassification,
            RecommendedOperatorAction = stability.RecommendedOperatorAction,
            HitRatio = stability.HitRatio,
            StaleServeRatio = stability.StaleServeRatio,
            BypassRatio = stability.BypassRatio,
            InvalidationChurn = stability.InvalidationChurn,
            RepeatedColdMisses = stability.RepeatedColdMisses,
            ReasonCodes = reasonCodes,
            TriggerSignals = OperationalCacheExplainabilityBuilder.BuildStabilityTriggerSignals(stability),
            GovernanceNotes = OperationalCacheExplainabilityBuilder.BuildReadinessNotes(
                overview.ReadinessState,
                overview.PressureSeverity),
            RecommendedActions = OperationalCacheExplainabilityBuilder.Bound(new[] { stability.RecommendedOperatorAction })
        };
    }
}
