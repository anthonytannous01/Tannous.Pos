namespace Tannous.Pos.Application.Audit;

/// <summary>Builds consolidated cache governance projections from metadata and telemetry.</summary>
public static class OperationalCacheGovernanceProjectionBuilder
{
    public static OperationalCacheGovernanceOverviewDto BuildOverview(
        IReadOnlyList<OperationalDiagnosticsCacheEntryMetadataDto> entries,
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry,
        OperationalCacheAdaptivePressureSignals pressureSignals)
    {
        var cardinalitySnapshot = OperationalCacheCardinalityClassifier.BuildSnapshot(entries);
        var pressureSeverity = OperationalCachePressureClassifier.Classify(
            telemetry,
            entries,
            cardinalitySnapshot.Classification);
        var stability = OperationalCacheStabilityClassifier.Compute(telemetry);
        var degradation = OperationalCacheDegradationClassifier.Classify(
            telemetry,
            stability,
            cardinalitySnapshot.Classification,
            pressureSeverity);
        var scopeDiagnostics = OperationalCacheScopeSurvivabilityBuilder.Build(entries, telemetry);

        var warmCandidates = OperationalCacheAdaptiveInsights.BuildWarmCandidates(
            telemetry,
            pressureSeverity);
        var suppressWarm = OperationalCachePressureClassifier.ShouldSuppressWarmRecommendations(pressureSeverity);
        var readiness = OperationalCacheAdaptiveInsights.ClassifyReadiness(
            telemetry,
            pressureSignals,
            entries.Count,
            warmCandidates,
            pressureSeverity);

        var dominantMode = OperationalCacheAdaptiveTtlClassifier.ClassifyTtlMode(pressureSignals);
        var total = telemetry.TotalHits + telemetry.TotalMisses;
        var hitRatio = total == 0 ? 0d : (double)telemetry.TotalHits / total;

        var atRisk = entries.Where(e => e.StaleRisk != OperationalDiagnosticsCacheStaleRisk.Fresh).ToList();

        var overviewReasonCodes = OperationalCacheExplainabilityBuilder.Bound(
            degradation.ReasonCodes.Concat(
                OperationalCacheExplainabilityBuilder.BuildPressureReasonCodes(
                    pressureSeverity,
                    telemetry,
                    cardinalitySnapshot.Classification)));

        return new OperationalCacheGovernanceOverviewDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            ReadinessState = readiness,
            PressureSeverity = pressureSeverity,
            DegradationState = degradation.State,
            CardinalityClassification = cardinalitySnapshot.Classification,
            DominantTtlMode = dominantMode,
            StabilityScore = stability.StabilityScore,
            StabilityClassification = stability.StabilityClassification,
            HitRatio = Math.Round(hitRatio, 4),
            TotalHits = telemetry.TotalHits,
            TotalMisses = telemetry.TotalMisses,
            TotalBypasses = telemetry.TotalBypasses,
            TotalInvalidations = telemetry.TotalInvalidations,
            ActiveEntryCount = entries.Count,
            ActiveScopedKeyCount = cardinalitySnapshot.ActiveScopedKeyCount,
            WarmCandidateCount = warmCandidates.Count,
            WarmRecommendationsSuppressed = suppressWarm,
            AgingEntryCount = atRisk.Count(e => e.StaleRisk == OperationalDiagnosticsCacheStaleRisk.Aging),
            NearExpiryEntryCount = atRisk.Count(e => e.StaleRisk == OperationalDiagnosticsCacheStaleRisk.NearExpiry),
            ExpiredEntryCount = atRisk.Count(e => e.StaleRisk == OperationalDiagnosticsCacheStaleRisk.Expired),
            Cardinality = cardinalitySnapshot,
            ScopeDiagnostics = scopeDiagnostics,
            Degradation = degradation,
            GovernanceNote = OperationalCacheAdaptiveGovernance.GetPredictiveWarmingAssumption()
                + " " + OperationalCacheCardinalityGovernance.GetAssumption(),
            ReasonCodes = overviewReasonCodes,
            TriggerSignals = OperationalCacheExplainabilityBuilder.Bound(new[]
            {
                $"TtlMode:{dominantMode}",
                $"Readiness:{readiness}",
                suppressWarm ? "WarmRecommendationsSuppressed" : string.Empty
            }),
            GovernanceNotes = OperationalCacheExplainabilityBuilder.BuildReadinessNotes(readiness, pressureSeverity),
            RecommendedActions = OperationalCacheExplainabilityBuilder.Bound(new[]
            {
                degradation.RecommendedOperatorAction,
                OperationalCachePressureClassifier.GetRecommendedAction(pressureSeverity)
            })
        };
    }

    public static OperationalCacheScopePressureDto BuildScopePressure(
        IReadOnlyList<OperationalDiagnosticsCacheEntryMetadataDto> entries,
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry)
    {
        var cardinality = OperationalCacheCardinalityClassifier.BuildSnapshot(entries);
        var severity = OperationalCachePressureClassifier.Classify(
            telemetry,
            entries,
            cardinality.Classification);

        var action = OperationalCachePressureClassifier.GetRecommendedAction(severity);
        var reasonCodes = OperationalCacheExplainabilityBuilder.BuildPressureReasonCodes(
            severity,
            telemetry,
            cardinality.Classification);

        return new OperationalCacheScopePressureDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            Severity = severity,
            Cardinality = cardinality.Classification,
            ScopedEntryRatio = cardinality.ScopedEntryRatio,
            RepeatedColdMisses = telemetry.RepeatedColdMisses,
            InvalidationChurn = telemetry.TotalInvalidations,
            RecommendedOperatorAction = action,
            ReasonCodes = reasonCodes,
            TriggerSignals = OperationalCacheExplainabilityBuilder.Bound(new[]
            {
                $"ScopedRatio:{cardinality.ScopedEntryRatio:F2}",
                telemetry.RepeatedColdMisses > 0 ? "FrequentColdMisses" : string.Empty
            }),
            GovernanceNotes = OperationalCacheExplainabilityBuilder.Bound(new[]
            {
                OperationalCacheGovernanceFinalizationGovernance.GetAssumption()
            }),
            RecommendedActions = OperationalCacheExplainabilityBuilder.Bound(new[] { action })
        };
    }
}
