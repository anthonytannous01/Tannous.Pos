namespace Tannous.Pos.Application.Audit;

/// <summary>Composes invalidation governance projections from cache metadata and telemetry only.</summary>
public static class OperationalCacheInvalidationProjectionBuilder
{
    public static OperationalCacheInvalidationAuditDto BuildAudit(
        IReadOnlyList<OperationalDiagnosticsCacheEntryMetadataDto> entries,
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry,
        OperationalDiagnosticsCacheDiagnosticsStaleRiskDto staleRisk)
    {
        var scopeDiagnostics = BuildScopeDiagnostics(entries, telemetry);
        var severity = OperationalCacheInvalidationSeverityClassifier.Classify(
            telemetry,
            entries.Count,
            scopeDiagnostics.ScopeChurnRatio,
            staleRisk.ExpiredEntryCount);
        var recovery = OperationalCacheFreshnessRecoveryClassifier.Classify(
            telemetry,
            staleRisk.AgingEntryCount,
            staleRisk.NearExpiryEntryCount,
            staleRisk.ExpiredEntryCount,
            entries.Count);
        var (drift, driftSignals) = OperationalCacheInvalidationDriftDetector.Detect(
            telemetry,
            entries.Count,
            scopeDiagnostics.ActiveScopedKeyCount,
            staleRisk.ExpiredEntryCount,
            scopeDiagnostics.ScopeChurnRatio);

        var affectedCategories = entries
            .Select(e => e.Category)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToList();

        var reasonCodes = OperationalCacheInvalidationExplainabilityBuilder.BuildInvalidationReasonCodes(
            severity,
            recovery,
            telemetry,
            scopeDiagnostics.ActiveScopedKeyCount,
            scopeDiagnostics.ScopeChurnRatio);

        var recommendations = OperationalCacheInvalidationRecommendationBuilder.Build(severity, recovery, drift);

        return new OperationalCacheInvalidationAuditDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            InvalidationSeverity = severity.ToString(),
            FreshnessRecoveryState = recovery.ToString(),
            InvalidationDriftClassification = drift.ToString(),
            TotalInvalidations = telemetry.TotalInvalidations,
            ScopedInvalidations = telemetry.ScopedInvalidations,
            CrossCategoryInvalidations = telemetry.CrossCategoryInvalidations,
            ScopedInvalidationRecoveries = telemetry.ScopedInvalidationRecoveries,
            FreshnessRecoveryCount = telemetry.FreshnessRecoveryCount,
            InvalidationDriftCount = telemetry.InvalidationDriftCount,
            InvalidationPressureEscalations = telemetry.InvalidationPressureEscalations,
            ActiveEntryCount = entries.Count,
            ActiveScopedKeyCount = scopeDiagnostics.ActiveScopedKeyCount,
            AgingEntryCount = staleRisk.AgingEntryCount,
            ExpiredEntryCount = staleRisk.ExpiredEntryCount,
            LastInvalidationUtc = telemetry.LastInvalidationUtc,
            AffectedCategories = affectedCategories,
            ReasonCodes = reasonCodes,
            TriggerSignals = OperationalCacheInvalidationExplainabilityBuilder.Bound(
                driftSignals.Concat(new[]
                {
                    $"ScopeChurn:{scopeDiagnostics.ScopeChurnRatio:F2}",
                    $"Recovery:{recovery}"
                })),
            Recommendations = recommendations,
            GovernanceNotes = OperationalCacheInvalidationExplainabilityBuilder.Bound(new[]
            {
                OperationalCacheInvalidationGovernance.GetAssumption(),
                OperationalCacheInvalidationGovernance.GetRecoveryAssumption()
            })
        };
    }

    public static OperationalCacheInvalidationConsistencyDto BuildConsistency(
        IReadOnlyList<OperationalDiagnosticsCacheEntryMetadataDto> entries,
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry,
        OperationalDiagnosticsCacheDiagnosticsStaleRiskDto staleRisk)
    {
        var scopeDiagnostics = BuildScopeDiagnostics(entries, telemetry);
        var (drift, signals) = OperationalCacheInvalidationDriftDetector.Detect(
            telemetry,
            entries.Count,
            scopeDiagnostics.ActiveScopedKeyCount,
            staleRisk.ExpiredEntryCount,
            scopeDiagnostics.ScopeChurnRatio);

        var isConsistent = drift == OperationalCacheInvalidationDriftClassification.None
                           || drift == OperationalCacheInvalidationDriftClassification.Minor;

        return new OperationalCacheInvalidationConsistencyDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            IsConsistent = isConsistent,
            InvalidationDriftClassification = drift.ToString(),
            InconsistencySignals = signals,
            ReasonCodes = OperationalCacheInvalidationExplainabilityBuilder.Bound(signals),
            GovernanceNotes = OperationalCacheInvalidationExplainabilityBuilder.Bound(new[]
            {
                isConsistent ? "Invalidation projections are within advisory tolerance." : "Advisory drift signals present; no auto-remediation.",
                OperationalCacheInvalidationGovernance.GetAssumption()
            })
        };
    }

    public static OperationalCacheFreshnessRecoveryDto BuildFreshnessRecovery(
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry,
        OperationalDiagnosticsCacheDiagnosticsStaleRiskDto staleRisk,
        int activeEntryCount)
    {
        var recovery = OperationalCacheFreshnessRecoveryClassifier.Classify(
            telemetry,
            staleRisk.AgingEntryCount,
            staleRisk.NearExpiryEntryCount,
            staleRisk.ExpiredEntryCount,
            activeEntryCount);

        var churnDenominator = Math.Max(1, activeEntryCount + telemetry.TotalInvalidations);
        var churnRatio = Math.Round((double)telemetry.TotalInvalidations / churnDenominator, 4);

        return new OperationalCacheFreshnessRecoveryDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            RecoveryState = recovery.ToString(),
            FreshnessRecoveryCount = telemetry.FreshnessRecoveryCount,
            TotalInvalidations = telemetry.TotalInvalidations,
            AgingEntryCount = staleRisk.AgingEntryCount,
            NearExpiryEntryCount = staleRisk.NearExpiryEntryCount,
            ExpiredEntryCount = staleRisk.ExpiredEntryCount,
            ActiveEntryCount = activeEntryCount,
            InvalidationChurnRatio = churnRatio,
            ReasonCodes = OperationalCacheInvalidationExplainabilityBuilder.Bound(new[]
            {
                $"Recovery{recovery}",
                telemetry.FreshnessRecoveryCount > 0 ? "FrequentFreshnessRecovery" : string.Empty,
                staleRisk.ExpiredEntryCount > 0 ? "ExpiredEntriesPresent" : string.Empty
            }),
            TriggerSignals = OperationalCacheInvalidationExplainabilityBuilder.Bound(new[]
            {
                $"Aging:{staleRisk.AgingEntryCount}",
                $"NearExpiry:{staleRisk.NearExpiryEntryCount}",
                $"Expired:{staleRisk.ExpiredEntryCount}",
                $"Recoveries:{telemetry.FreshnessRecoveryCount}"
            }),
            GovernanceNotes = OperationalCacheInvalidationExplainabilityBuilder.Bound(new[]
            {
                OperationalCacheInvalidationGovernance.GetRecoveryAssumption()
            })
        };
    }

    public static OperationalCacheInvalidationPressureDto BuildPressure(
        IReadOnlyList<OperationalDiagnosticsCacheEntryMetadataDto> entries,
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry)
    {
        var scopeDiagnostics = BuildScopeDiagnostics(entries, telemetry);
        var severity = OperationalCacheInvalidationSeverityClassifier.Classify(
            telemetry,
            entries.Count,
            scopeDiagnostics.ScopeChurnRatio,
            entries.Count(e => e.StaleRisk == OperationalDiagnosticsCacheStaleRisk.Expired));

        var churnDenominator = Math.Max(1, entries.Count + telemetry.TotalInvalidations);
        var invalidationChurnRatio = Math.Round((double)telemetry.TotalInvalidations / churnDenominator, 4);

        var byCategory = entries
            .GroupBy(e => e.Category, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        return new OperationalCacheInvalidationPressureDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            InvalidationSeverity = severity.ToString(),
            TotalInvalidations = telemetry.TotalInvalidations,
            ScopedInvalidations = telemetry.ScopedInvalidations,
            CrossCategoryInvalidations = telemetry.CrossCategoryInvalidations,
            InvalidationPressureEscalations = telemetry.InvalidationPressureEscalations,
            ScopeChurnRatio = scopeDiagnostics.ScopeChurnRatio,
            InvalidationChurnRatio = invalidationChurnRatio,
            ActiveScopedKeyCount = scopeDiagnostics.ActiveScopedKeyCount,
            InvalidationsByCategoryEstimate = byCategory,
            ReasonCodes = OperationalCacheInvalidationExplainabilityBuilder.BuildInvalidationReasonCodes(
                severity,
                OperationalCacheFreshnessRecoveryState.Stable,
                telemetry,
                scopeDiagnostics.ActiveScopedKeyCount,
                scopeDiagnostics.ScopeChurnRatio),
            GovernanceNotes = OperationalCacheInvalidationExplainabilityBuilder.Bound(new[]
            {
                OperationalCacheInvalidationGovernance.GetAssumption()
            })
        };
    }

    public static OperationalCacheInvalidationScopeDiagnosticsDto BuildScopeDiagnostics(
        IReadOnlyList<OperationalDiagnosticsCacheEntryMetadataDto> entries,
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry)
    {
        var scopedEntries = entries
            .Where(e => !string.Equals(e.Scope, OperationalDiagnosticsCacheScopes.Global, StringComparison.Ordinal))
            .ToList();

        var activeScopedKeyCount = scopedEntries.Count;
        var scopeChurnRatio = telemetry.TotalInvalidations == 0
            ? 0d
            : Math.Round(
                (double)telemetry.ScopedInvalidations / Math.Max(1, telemetry.TotalInvalidations),
                4);

        var scopedByCategory = scopedEntries
            .GroupBy(e => e.Category, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        var oldestScoped = scopedEntries
            .OrderByDescending(e => e.AgeSeconds)
            .Take(8)
            .Select(e => new OperationalCacheScopedEntrySurvivabilityDto
            {
                Category = e.Category,
                Scope = e.Scope,
                CacheKeyAlias = e.CacheKeyAlias,
                AgeSeconds = e.AgeSeconds,
                RemainingTtlSeconds = e.RemainingTtlSeconds
            })
            .ToList();

        return new OperationalCacheInvalidationScopeDiagnosticsDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            ActiveScopedKeyCount = activeScopedKeyCount,
            ScopedInvalidations = telemetry.ScopedInvalidations,
            ScopedInvalidationRecoveries = telemetry.ScopedInvalidationRecoveries,
            ScopeChurnRatio = scopeChurnRatio,
            ScopedEntriesByCategory = scopedByCategory,
            OldestScopedEntries = oldestScoped,
            ReasonCodes = OperationalCacheInvalidationExplainabilityBuilder.Bound(new[]
            {
                activeScopedKeyCount > 0 ? "ScopedKeysActive" : string.Empty,
                scopeChurnRatio >= OperationalCacheInvalidationGovernance.ElevatedScopeChurnRatio
                    ? "HighScopedInvalidationChurn"
                    : string.Empty
            }),
            GovernanceNote = OperationalCacheInvalidationGovernance.GetAssumption()
        };
    }
}
