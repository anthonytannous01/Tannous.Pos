using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Audit.Governance;

namespace Tannous.Pos.Infrastructure.Services.OperationalDiagnosticsProjections;

internal sealed class OperationalDiagnosticsCacheGovernanceProjectionCollaborator
{
    private readonly OperationalDiagnosticsCacheProjectionContextFactory _contextFactory;
    private readonly ILogger _logger;

    public OperationalDiagnosticsCacheGovernanceProjectionCollaborator(
        OperationalDiagnosticsCacheProjectionContextFactory contextFactory,
        ILogger logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public OperationalDiagnosticsCacheDiagnosticsSummaryDto GetSummary()
    {
        _logger.LogInformation("Operational cache diagnostics: summary query executed.");

        var entries = _contextFactory.GetEntries();
        var ages = entries.Select(static e => e.AgeSeconds).ToList();
        var telemetry = _contextFactory.GetTelemetry();

        return new OperationalDiagnosticsCacheDiagnosticsSummaryDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            ActiveEntryCount = entries.Count,
            MaxCacheEntryCount = OperationalDiagnosticsCacheConstants.MaxCacheEntryCount,
            OldestEntryAgeSeconds = ages.Count == 0 ? null : ages.Max(),
            NewestEntryAgeSeconds = ages.Count == 0 ? null : ages.Min(),
            TotalInvalidations = telemetry.TotalInvalidations,
            LastInvalidationUtc = telemetry.LastInvalidationUtc,
            ActiveScopedKeyCount = entries.Count(e =>
                !string.Equals(e.Scope, OperationalDiagnosticsCacheScopes.Global, StringComparison.Ordinal)),
            Entries = entries,
            EntriesByCategory = entries
                .GroupBy(e => e.Category, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal),
            EntriesByScope = entries
                .GroupBy(e => e.Scope, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal),
            CategoryTtlSeconds = BuildCategoryTtlMap(),
            GovernanceNote = OperationalDiagnosticsCacheGovernance.GetStaleReadExpectation()
        };
    }

    public OperationalDiagnosticsCacheDiagnosticsPressureDto GetPressure()
    {
        var snapshot = _contextFactory.GetTelemetry();
        var pressureState = _contextFactory.PressureState;
        var bypassesByCategory = snapshot.ByCategory.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Bypasses,
            StringComparer.Ordinal);

        var pressure = new OperationalDiagnosticsCacheDiagnosticsPressureDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            TotalBypasses = snapshot.TotalBypasses,
            QueryDateRangeClamped = pressureState.QueryDateRangeClamped,
            QueryPageSizeClamped = pressureState.QueryPageSizeClamped,
            ForensicExportTruncated = pressureState.ForensicExportTruncated,
            BypassesByCategory = bypassesByCategory,
            PressureNote = OperationalDiagnosticsCacheGovernance.GetMultiInstanceAssumption()
        };

        _logger.LogWarning(
            "Operational cache pressure visibility: pressure diagnostics queried. TotalBypasses={TotalBypasses}, QueryDateRangeClamped={QueryDateRangeClamped}, QueryPageSizeClamped={QueryPageSizeClamped}, ForensicExportTruncated={ForensicExportTruncated}",
            pressure.TotalBypasses,
            pressure.QueryDateRangeClamped,
            pressure.QueryPageSizeClamped,
            pressure.ForensicExportTruncated);

        return pressure;
    }

    public OperationalDiagnosticsCacheDiagnosticsStaleRiskDto GetStaleRisk()
    {
        var dto = OperationalGovernanceStaleRiskProjectionBuilder.Build(_contextFactory.GetEntries());

        _logger.LogInformation(
            "Operational cache stale visibility: stale-risk query executed. Aging={Aging}, NearExpiry={NearExpiry}, Expired={Expired}",
            dto.AgingEntryCount,
            dto.NearExpiryEntryCount,
            dto.ExpiredEntryCount);

        return dto;
    }

    public OperationalDiagnosticsCacheDiagnosticsEffectivenessDto GetEffectiveness()
    {
        var snapshot = _contextFactory.GetTelemetry();
        var hitRatio = OperationalGovernanceThresholdEvaluator.ComputeHitRatio(
            snapshot.TotalHits,
            snapshot.TotalMisses);

        var dto = new OperationalDiagnosticsCacheDiagnosticsEffectivenessDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            TotalHits = snapshot.TotalHits,
            TotalMisses = snapshot.TotalMisses,
            TotalBypasses = snapshot.TotalBypasses,
            TotalStaleServes = snapshot.TotalStaleServes,
            HitRatio = Math.Round(hitRatio, 4),
            TotalInvalidations = snapshot.TotalInvalidations,
            LastInvalidationUtc = snapshot.LastInvalidationUtc,
            ByCategory = snapshot.ByCategory
        };

        _logger.LogInformation(
            "Operational cache effectiveness: effectiveness snapshot queried. TotalHits={TotalHits}, TotalMisses={TotalMisses}, TotalBypasses={TotalBypasses}, TotalStaleServes={TotalStaleServes}, HitRatio={HitRatio}",
            dto.TotalHits,
            dto.TotalMisses,
            dto.TotalBypasses,
            dto.TotalStaleServes,
            dto.HitRatio);

        return dto;
    }

    public OperationalCacheAdaptiveSummaryDto GetAdaptiveSummary()
    {
        var context = _contextFactory.BuildFullContext();
        var telemetry = context.Telemetry;
        var entries = context.Entries;
        var pressureSignals = context.PressureSignals;
        var pressureSeverity = context.Overview.PressureSeverity;
        var suppressWarm = OperationalCachePressureClassifier.ShouldSuppressWarmRecommendations(pressureSeverity);
        var warmCandidates = OperationalCacheAdaptiveInsights.BuildWarmCandidates(telemetry, pressureSeverity);
        var readiness = context.ReadinessState;
        var dominantMode = OperationalCacheAdaptiveTtlClassifier.ClassifyTtlMode(pressureSignals);

        var effectiveTtl = new Dictionary<string, int>(StringComparer.Ordinal);
        var ttlModes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var category in GetTrackedAdaptiveCategories())
        {
            var mode = OperationalCacheAdaptiveTtlClassifier.ClassifyTtlMode(pressureSignals);
            effectiveTtl[category] = (int)OperationalCacheAdaptiveTtlClassifier
                .GetAdaptiveTtl(category, mode)
                .TotalSeconds;
            ttlModes[category] = mode.ToString();
        }

        var adaptiveAction = OperationalCachePressureClassifier.GetRecommendedAction(pressureSeverity);
        var dto = new OperationalCacheAdaptiveSummaryDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            ReadinessState = readiness,
            DominantTtlMode = dominantMode,
            WarmCandidateCount = warmCandidates.Count,
            WarmestCategories = OperationalCacheAdaptiveInsights.GetWarmestCategories(warmCandidates),
            WarmRecommendations = telemetry.WarmRecommendations,
            RepeatedColdMisses = telemetry.RepeatedColdMisses,
            AdaptiveTtlReductions = telemetry.AdaptiveTtlReductions,
            EffectiveTtlSecondsByCategory = effectiveTtl,
            TtlModeByCategory = ttlModes,
            GovernanceNote = OperationalCacheAdaptiveGovernance.GetPredictiveWarmingAssumption(),
            ReasonCodes = OperationalCacheExplainabilityBuilder.Bound(new[]
            {
                $"TtlMode:{dominantMode}",
                $"Readiness:{readiness}",
                suppressWarm ? "WarmRecommendationsSuppressed" : string.Empty
            }),
            TriggerSignals = OperationalCacheExplainabilityBuilder.Bound(new[]
            {
                telemetry.AdaptiveTtlReductions > 0 ? "AdaptiveTtlReduction" : string.Empty,
                telemetry.RepeatedColdMisses > 0 ? "FrequentColdMisses" : string.Empty
            }),
            GovernanceNotes = OperationalCacheExplainabilityBuilder.BuildReadinessNotes(readiness, pressureSeverity),
            RecommendedActions = OperationalCacheExplainabilityBuilder.Bound(new[] { adaptiveAction })
        };

        _logger.LogInformation(
            "Operational adaptive cache governance: adaptive summary queried. Readiness={Readiness}, DominantTtlMode={DominantTtlMode}, WarmCandidates={WarmCandidates}",
            readiness,
            dominantMode,
            warmCandidates.Count);

        return dto;
    }

    public OperationalCacheWarmCandidatesDiagnosticsDto GetWarmCandidates()
    {
        var context = _contextFactory.BuildFullContext();
        var candidates = OperationalCacheAdaptiveInsights.BuildWarmCandidates(
            context.Telemetry,
            context.Overview.PressureSeverity);

        _logger.LogInformation(
            "Operational cache warming visibility: warm-candidate query executed. WarmCandidateCount={WarmCandidateCount}",
            candidates.Count);

        return new OperationalCacheWarmCandidatesDiagnosticsDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            WarmCandidateCount = candidates.Count,
            Candidates = candidates,
            GovernanceNote = OperationalCacheAdaptiveGovernance.GetPredictiveWarmingAssumption()
        };
    }

    public OperationalCacheStabilityDto GetStability()
    {
        var dto = _contextFactory.BuildFullContext().Stability;

        _logger.LogInformation(
            "Operational cache stability: stability snapshot queried. Score={Score}, Classification={Classification}",
            dto.StabilityScore,
            dto.StabilityClassification);

        return dto;
    }

    public OperationalCacheGovernanceOverviewDto GetGovernanceOverview()
    {
        var access = _contextFactory.AcquireSnapshot();
        var overview = access.Composition.Context.Overview;
        var telemetrySnapshot = _contextFactory.GetTelemetry();
        var reuseRatio = OperationalGovernanceProjectionReuseClassifier.ComputeHitRatio(
            telemetrySnapshot.ProjectionReuseHits,
            telemetrySnapshot.ProjectionReuseMisses);
        var productionReadiness = OperationalGovernanceProductionReadinessClassifier.Classify(
            access.Composition.Context,
            telemetrySnapshot,
            reuseRatio,
            telemetrySnapshot.GovernanceFingerprintTransitions);

        var enriched = new OperationalCacheGovernanceOverviewDto
        {
            GeneratedAtUtc = overview.GeneratedAtUtc,
            ReadinessState = overview.ReadinessState,
            PressureSeverity = overview.PressureSeverity,
            DegradationState = overview.DegradationState,
            CardinalityClassification = overview.CardinalityClassification,
            DominantTtlMode = overview.DominantTtlMode,
            StabilityScore = overview.StabilityScore,
            StabilityClassification = overview.StabilityClassification,
            HitRatio = overview.HitRatio,
            TotalHits = overview.TotalHits,
            TotalMisses = overview.TotalMisses,
            TotalBypasses = overview.TotalBypasses,
            TotalInvalidations = overview.TotalInvalidations,
            ActiveEntryCount = overview.ActiveEntryCount,
            ActiveScopedKeyCount = overview.ActiveScopedKeyCount,
            WarmCandidateCount = overview.WarmCandidateCount,
            WarmRecommendationsSuppressed = overview.WarmRecommendationsSuppressed,
            AgingEntryCount = overview.AgingEntryCount,
            NearExpiryEntryCount = overview.NearExpiryEntryCount,
            ExpiredEntryCount = overview.ExpiredEntryCount,
            Cardinality = overview.Cardinality,
            ScopeDiagnostics = overview.ScopeDiagnostics,
            Degradation = overview.Degradation,
            GovernanceNote = overview.GovernanceNote,
            ReasonCodes = overview.ReasonCodes,
            TriggerSignals = overview.TriggerSignals,
            GovernanceNotes = overview.GovernanceNotes,
            RecommendedActions = overview.RecommendedActions,
            ProductionReadiness = productionReadiness
        };

        _logger.LogInformation(
            "Operational cache governance overview: governance overview queried. Readiness={Readiness}, PressureSeverity={PressureSeverity}, Degradation={Degradation}, Cardinality={Cardinality}, WarmCandidates={WarmCandidates}, Suppressed={Suppressed}, ProductionReadiness={ProductionReadiness}",
            enriched.ReadinessState,
            enriched.PressureSeverity,
            enriched.DegradationState,
            enriched.CardinalityClassification,
            enriched.WarmCandidateCount,
            enriched.WarmRecommendationsSuppressed,
            enriched.ProductionReadiness.ReadinessState);

        return enriched;
    }

    public OperationalCacheCardinalitySnapshotDto GetCardinalitySnapshot()
    {
        var snapshot = _contextFactory.BuildFullContext().Cardinality;

        _logger.LogInformation(
            "Operational cache cardinality: cardinality snapshot queried. Classification={Classification}, ActiveEntries={ActiveEntries}, ScopedKeys={ScopedKeys}",
            snapshot.Classification,
            snapshot.ActiveEntryCount,
            snapshot.ActiveScopedKeyCount);

        return snapshot;
    }

    public OperationalCacheDegradationDto GetDegradation()
    {
        var context = _contextFactory.BuildFullContext();
        var degradation = OperationalCacheDegradationClassifier.Classify(
            context.Telemetry,
            context.Stability,
            context.Cardinality.Classification,
            context.Overview.PressureSeverity);

        _logger.LogInformation(
            "Operational cache degradation: degradation snapshot queried. State={State}, ExcessiveBypass={ExcessiveBypass}, SaturatedScoped={SaturatedScoped}",
            degradation.State,
            degradation.ExcessiveBypassIndicated,
            degradation.SaturatedScopedKeysIndicated);

        _logger.LogInformation(
            "Operational cache pressure classification: pressure severity={Severity}",
            context.Overview.PressureSeverity);

        return degradation;
    }

    public OperationalCacheGovernanceAuditDto GetGovernanceAudit()
    {
        var context = _contextFactory.BuildFullContext();
        var audit = OperationalCacheGovernanceAuditBuilder.Build(
            context.Entries,
            context.Telemetry,
            context.PressureSignals);

        _logger.LogInformation(
            "Operational cache governance audit: governance audit queried. Pressure={Pressure}, Degradation={Degradation}, DriftDetected={DriftDetected}, Survivability={Survivability}",
            audit.PressureSeverity,
            audit.DegradationState,
            audit.Drift.DriftDetected,
            audit.SurvivabilityClassification);

        LogRecommendations(audit.Recommendations, "Operational operator guidance");

        return audit;
    }

    public OperationalCacheGovernanceConsistencyDto GetGovernanceConsistency()
    {
        var context = _contextFactory.BuildFullContext();
        var consistency = OperationalCacheGovernanceConsistencyValidator.Validate(
            context.Overview,
            context.Survivability);

        _logger.LogInformation(
            "Operational diagnostics consistency: consistency validation executed. IsConsistent={IsConsistent}, SignalCount={SignalCount}",
            consistency.IsConsistent,
            consistency.InconsistencySignals.Count);

        if (consistency.InconsistencySignals.Count > 0)
        {
            _logger.LogWarning(
                "Operational governance drift visibility: consistency drift signals present. Severity=Advisory, SignalCount={SignalCount}",
                consistency.InconsistencySignals.Count);
        }

        return consistency;
    }

    private void LogRecommendations(
        IReadOnlyList<OperationalCacheGovernanceRecommendationDto> recommendations,
        string prefix)
    {
        foreach (var recommendation in recommendations.Take(3))
        {
            _logger.LogInformation(
                "{Prefix}: recommendation emitted. Code={Code}, Priority={Priority}",
                prefix,
                recommendation.Code,
                recommendation.Priority);
        }
    }

    private static IEnumerable<string> GetTrackedAdaptiveCategories() =>
    [
        OperationalDiagnosticsCacheCategories.ResilienceMetrics,
        OperationalDiagnosticsCacheCategories.ReconciliationSummary,
        OperationalDiagnosticsCacheCategories.IncidentGroups,
        OperationalDiagnosticsCacheCategories.AlertSignals,
        OperationalDiagnosticsCacheCategories.AlertSummary
    ];

    private static IReadOnlyDictionary<string, int> BuildCategoryTtlMap() =>
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [OperationalDiagnosticsCacheCategories.ResilienceMetrics] =
                OperationalDiagnosticsCacheConstants.ResilienceMetricsTtlSeconds,
            [OperationalDiagnosticsCacheCategories.ReconciliationSummary] =
                OperationalDiagnosticsCacheConstants.ReconciliationSummaryTtlSeconds,
            [OperationalDiagnosticsCacheCategories.IncidentGroups] =
                OperationalDiagnosticsCacheConstants.IncidentGroupsTtlSeconds,
            [OperationalDiagnosticsCacheCategories.IncidentSummary] =
                OperationalDiagnosticsCacheConstants.IncidentSummaryTtlSeconds,
            [OperationalDiagnosticsCacheCategories.AlertSignals] =
                OperationalDiagnosticsCacheConstants.AlertSignalsTtlSeconds,
            [OperationalDiagnosticsCacheCategories.AlertSummary] =
                OperationalDiagnosticsCacheConstants.AlertSummaryTtlSeconds,
            [OperationalDiagnosticsCacheCategories.ForensicSnapshotSummary] =
                OperationalDiagnosticsCacheConstants.ForensicSnapshotSummaryTtlSeconds
        };
}
