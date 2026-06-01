namespace Tannous.Pos.Application.Audit;

/// <summary>Composes Step 11 consistency recovery & containment projections (metadata/telemetry only).</summary>
public static class OperationalCacheConsistencyProjectionBuilder
{
    public static OperationalCacheConsistencyRecoveryDto BuildRecovery(
        IReadOnlyList<OperationalDiagnosticsCacheEntryMetadataDto> entries,
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry,
        OperationalDiagnosticsCacheDiagnosticsStaleRiskDto staleRisk,
        OperationalCacheGovernanceOverviewDto overview,
        OperationalCacheStabilityDto stability,
        OperationalCacheSurvivabilityDto survivability,
        OperationalCacheAdaptivePressureSignals pressureSignals)
    {
        var (propagation, _, _) = OperationalCachePropagationDetector.Detect(entries, telemetry);
        var containment = OperationalCacheRecoveryContainmentClassifier.Classify(
            telemetry,
            overview,
            propagation,
            staleRisk.ExpiredEntryCount,
            entries.Count);
        var (confidence, _) = OperationalCacheConsistencyConfidenceClassifier.Classify(
            telemetry,
            stability,
            overview,
            staleRisk.ExpiredEntryCount);

        var total = telemetry.TotalHits + telemetry.TotalMisses;
        var hitRatio = total == 0 ? 0d : Math.Round((double)telemetry.TotalHits / total, 4);
        var churnDenominator = Math.Max(1, entries.Count + telemetry.TotalInvalidations);
        var churnRatio = Math.Round((double)telemetry.TotalInvalidations / churnDenominator, 4);

        var window = OperationalCacheRecoveryWindowClassifier.Classify(
            telemetry,
            staleRisk,
            entries.Count,
            hitRatio);

        var recommendations = OperationalCacheContainmentRecommendationBuilder.Build(
            containment,
            confidence,
            propagation,
            window);

        var reasonCodes = OperationalCacheConsistencyExplainabilityBuilder.BuildConsistencyReasonCodes(
            confidence,
            containment,
            propagation,
            telemetry,
            window);

        return new OperationalCacheConsistencyRecoveryDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            ContainmentState = containment.ToString(),
            ConfidenceLevel = confidence.ToString(),
            ConsistencyRecoveryCycles = telemetry.ConsistencyRecoveryCycles,
            RecoveryWindowExtensions = telemetry.RecoveryWindowExtensions,
            ActiveEntryCount = entries.Count,
            ExpiredEntryCount = staleRisk.ExpiredEntryCount,
            HitRatio = hitRatio,
            InvalidationChurnRatio = churnRatio,
            RecoveryWindow = window,
            ReasonCodes = reasonCodes,
            TriggerSignals = OperationalCacheConsistencyExplainabilityBuilder.Bound(new[]
            {
                $"Containment:{containment}",
                $"Propagation:{propagation}",
                $"Survivability:{survivability.ClassificationLabel}",
                pressureSignals.QueryDateRangeClamped ? "QueryPressure" : string.Empty
            }),
            Recommendations = recommendations,
            GovernanceNotes = OperationalCacheConsistencyExplainabilityBuilder.Bound(new[]
            {
                OperationalCacheConsistencyGovernance.GetAssumption(),
                OperationalCacheConsistencyGovernance.GetContainmentAssumption()
            })
        };
    }

    public static OperationalCacheContainmentAuditDto BuildContainmentAudit(
        IReadOnlyList<OperationalDiagnosticsCacheEntryMetadataDto> entries,
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry,
        OperationalDiagnosticsCacheDiagnosticsStaleRiskDto staleRisk,
        OperationalCacheGovernanceOverviewDto overview,
        OperationalCacheStabilityDto stability)
    {
        var (propagation, propagationSignals, _) = OperationalCachePropagationDetector.Detect(entries, telemetry);
        var containment = OperationalCacheRecoveryContainmentClassifier.Classify(
            telemetry,
            overview,
            propagation,
            staleRisk.ExpiredEntryCount,
            entries.Count);
        var (confidence, confidenceScore) = OperationalCacheConsistencyConfidenceClassifier.Classify(
            telemetry,
            stability,
            overview,
            staleRisk.ExpiredEntryCount);

        var affectedCategories = entries
            .Select(e => e.Category)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToList();

        var window = OperationalCacheRecoveryWindowClassifier.Classify(
            telemetry,
            staleRisk,
            entries.Count,
            overview.HitRatio);

        var recommendations = OperationalCacheContainmentRecommendationBuilder.Build(
            containment,
            confidence,
            propagation,
            window);

        return new OperationalCacheContainmentAuditDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            ContainmentState = containment.ToString(),
            PropagationSeverity = propagation.ToString(),
            ConfidenceLevel = confidence.ToString(),
            ContainmentEscalations = telemetry.ContainmentEscalations,
            PropagationDetections = telemetry.PropagationDetections,
            ConsistencyConfidenceDrops = telemetry.ConsistencyConfidenceDrops,
            StabilityScore = stability.StabilityScore,
            DegradationState = overview.DegradationState.ToString(),
            PressureSeverity = overview.PressureSeverity.ToString(),
            AffectedCategories = affectedCategories,
            ReasonCodes = OperationalCacheConsistencyExplainabilityBuilder.BuildConsistencyReasonCodes(
                confidence,
                containment,
                propagation,
                telemetry,
                window),
            TriggerSignals = OperationalCacheConsistencyExplainabilityBuilder.Bound(
                propagationSignals.Concat(new[] { $"ConfidenceScore:{confidenceScore}" })),
            Recommendations = recommendations,
            GovernanceNotes = OperationalCacheConsistencyExplainabilityBuilder.Bound(new[]
            {
                OperationalCacheConsistencyGovernance.GetContainmentAssumption()
            })
        };
    }

    public static OperationalCachePropagationDiagnosticsDto BuildPropagationDiagnostics(
        IReadOnlyList<OperationalDiagnosticsCacheEntryMetadataDto> entries,
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry)
    {
        var (severity, signals, exposure) = OperationalCachePropagationDetector.Detect(entries, telemetry);

        return new OperationalCachePropagationDiagnosticsDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            PropagationSeverity = severity.ToString(),
            PropagationDetections = telemetry.PropagationDetections,
            CrossCategoryInvalidations = telemetry.CrossCategoryInvalidations,
            InvalidationDriftCount = telemetry.InvalidationDriftCount,
            CategoryExposureCounts = exposure
                .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.Ordinal),
            PropagationSignals = signals,
            ReasonCodes = OperationalCacheConsistencyExplainabilityBuilder.Bound(signals),
            GovernanceNotes = OperationalCacheConsistencyExplainabilityBuilder.Bound(new[]
            {
                OperationalCacheConsistencyGovernance.GetAssumption()
            })
        };
    }

    public static OperationalCacheConsistencyConfidenceDto BuildConfidence(
        IReadOnlyList<OperationalDiagnosticsCacheEntryMetadataDto> entries,
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry,
        OperationalDiagnosticsCacheDiagnosticsStaleRiskDto staleRisk,
        OperationalCacheGovernanceOverviewDto overview,
        OperationalCacheStabilityDto stability,
        OperationalCacheSurvivabilityDto survivability)
    {
        var (confidence, score) = OperationalCacheConsistencyConfidenceClassifier.Classify(
            telemetry,
            stability,
            overview,
            staleRisk.ExpiredEntryCount);

        var total = telemetry.TotalHits + telemetry.TotalMisses;
        var hitRatio = total == 0 ? 0d : Math.Round((double)telemetry.TotalHits / total, 4);
        var bypassDenom = total + telemetry.TotalBypasses;
        var bypassRatio = bypassDenom == 0 ? 0d : Math.Round((double)telemetry.TotalBypasses / bypassDenom, 4);
        var staleRatio = total == 0 ? 0d : Math.Round((double)telemetry.TotalStaleServes / total, 4);

        var window = OperationalCacheRecoveryWindowClassifier.Classify(
            telemetry,
            staleRisk,
            entries.Count,
            hitRatio);

        var (propagation, _, _) = OperationalCachePropagationDetector.Detect(entries, telemetry);
        var containment = OperationalCacheRecoveryContainmentClassifier.Classify(
            telemetry,
            overview,
            propagation,
            staleRisk.ExpiredEntryCount,
            entries.Count);

        return new OperationalCacheConsistencyConfidenceDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            ConfidenceLevel = confidence.ToString(),
            ConfidenceScore = score,
            ConsistencyConfidenceDrops = telemetry.ConsistencyConfidenceDrops,
            HitRatio = hitRatio,
            BypassRatio = bypassRatio,
            StaleServeRatio = staleRatio,
            StabilityScore = stability.StabilityScore,
            SurvivabilityClassification = survivability.ClassificationLabel,
            RecoveryWindow = window,
            ReasonCodes = OperationalCacheConsistencyExplainabilityBuilder.BuildConsistencyReasonCodes(
                confidence,
                containment,
                propagation,
                telemetry,
                window),
            TriggerSignals = OperationalCacheConsistencyExplainabilityBuilder.Bound(new[]
            {
                $"Score:{score}",
                $"Stability:{stability.StabilityClassification}",
                window.StabilizationAchieved ? "StabilizationAchieved" : string.Empty
            }),
            GovernanceNotes = OperationalCacheConsistencyExplainabilityBuilder.Bound(new[]
            {
                OperationalCacheConsistencyGovernance.GetAssumption()
            })
        };
    }
}
