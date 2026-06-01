using System.Collections.Concurrent;
using Tannous.Pos.Application.Audit;

namespace Tannous.Pos.Infrastructure.Services;

/// <summary>In-process operational diagnostics cache telemetry (not persisted).</summary>
public sealed class OperationalDiagnosticsCacheTelemetry : IOperationalDiagnosticsCacheTelemetry
{
    private long _totalHits;
    private long _totalMisses;
    private long _totalBypasses;
    private long _totalStaleServes;
    private long _totalInvalidations;
    private long _warmRecommendations;
    private long _repeatedColdMisses;
    private long _adaptiveTtlReductions;
    private long _scopedInvalidations;
    private long _crossCategoryInvalidations;
    private long _scopedInvalidationRecoveries;
    private long _freshnessRecoveryCount;
    private long _invalidationDriftCount;
    private long _invalidationPressureEscalations;
    private long _consistencyRecoveryCycles;
    private long _containmentEscalations;
    private long _propagationDetections;
    private long _recoveryWindowExtensions;
    private long _consistencyConfidenceDrops;
    private long _pressureRecoveryCycles;
    private long _pressureLifecycleTransitions;
    private long _pressureConvergenceRecoveries;
    private long _stickyPressureRecoveries;
    private long _stabilizationWindowResets;
    private long _adaptiveTtlRecoveries;
    private long _governanceFailsafeActivations;
    private long _runtimeBudgetConstrainedEvents;
    private long _projectionComplexityElevations;
    private long _telemetrySaturationEvents;
    private long _explainabilityTruncations;
    private long _governanceSnapshotBuilds;
    private long _governanceSnapshotReuses;
    private long _projectionReuseHits;
    private long _projectionReuseMisses;
    private long _snapshotConsistencyTransitions;
    private long _governanceFingerprintTransitions;
    private long _governanceStableFingerprintHits;
    private long _governanceDriftEscalations;
    private long _replayConsistencyChecks;
    private long _projectionFragmentationSignals;
    private long _compositionReuseHits;
    private long _compositionReuseMisses;
    private long _compositionNestedReadAvoidanceCount;
    private long _compositionSnapshotBuilds;
    private DateTime? _lastInvalidationUtc;

    private readonly ConcurrentDictionary<string, CategoryCounters> _byCategory = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, long> _repeatedColdMissesByCategory = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, long> _recentMissStreakByCategory = new(StringComparer.Ordinal);

    public void RecordHit(string category)
    {
        Interlocked.Increment(ref _totalHits);
        var stats = GetCategory(category);
        stats.IncrementHit();
        _recentMissStreakByCategory.TryRemove(category, out _);

        if (Volatile.Read(ref stats.Hits) >= 2 && Volatile.Read(ref stats.Misses) >= 1)
            RecordWarmRecommendation(category);
    }

    public void RecordMiss(string category)
    {
        Interlocked.Increment(ref _totalMisses);
        GetCategory(category).IncrementMiss();

        var streak = _recentMissStreakByCategory.AddOrUpdate(category, 1, static (_, current) => current + 1);
        if (streak >= 2)
        {
            RecordRepeatedColdMiss(category);
            _recentMissStreakByCategory[category] = 0;
        }
    }

    public void RecordAdaptiveTtlReduction(string category) =>
        Interlocked.Increment(ref _adaptiveTtlReductions);

    public void RecordRepeatedColdMiss(string category)
    {
        Interlocked.Increment(ref _repeatedColdMisses);
        _repeatedColdMissesByCategory.AddOrUpdate(category, 1, static (_, current) => current + 1);
    }

    public void RecordWarmRecommendation(string category) =>
        Interlocked.Increment(ref _warmRecommendations);

    public void RecordBypass(string category)
    {
        Interlocked.Increment(ref _totalBypasses);
        GetCategory(category).IncrementBypass();
    }

    public void RecordStaleServe(string category, OperationalDiagnosticsCacheStaleRisk staleRisk)
    {
        Interlocked.Increment(ref _totalStaleServes);
        GetCategory(category).IncrementStaleServe();
    }

    public void RecordInvalidation(string category, int removedCount, bool scopedKey = false)
    {
        if (removedCount <= 0)
            return;

        Interlocked.Add(ref _totalInvalidations, removedCount);
        if (scopedKey)
            Interlocked.Add(ref _scopedInvalidations, removedCount);
        _lastInvalidationUtc = DateTime.UtcNow;
    }

    public void RecordCrossCategoryInvalidation(int categoriesAffected)
    {
        if (categoriesAffected < 2)
            return;

        Interlocked.Increment(ref _crossCategoryInvalidations);
    }

    public void RecordScopedInvalidationRecovery() =>
        Interlocked.Increment(ref _scopedInvalidationRecoveries);

    public void RecordFreshnessRecovery() =>
        Interlocked.Increment(ref _freshnessRecoveryCount);

    public void RecordInvalidationDrift() =>
        Interlocked.Increment(ref _invalidationDriftCount);

    public void RecordInvalidationPressureEscalation() =>
        Interlocked.Increment(ref _invalidationPressureEscalations);

    public void RecordConsistencyRecoveryCycle() =>
        Interlocked.Increment(ref _consistencyRecoveryCycles);

    public void RecordContainmentEscalation() =>
        Interlocked.Increment(ref _containmentEscalations);

    public void RecordPropagationDetection() =>
        Interlocked.Increment(ref _propagationDetections);

    public void RecordRecoveryWindowExtension() =>
        Interlocked.Increment(ref _recoveryWindowExtensions);

    public void RecordConsistencyConfidenceDrop() =>
        Interlocked.Increment(ref _consistencyConfidenceDrops);

    public void RecordPressureRecoveryCycle() =>
        Interlocked.Increment(ref _pressureRecoveryCycles);

    public void RecordPressureLifecycleTransition() =>
        Interlocked.Increment(ref _pressureLifecycleTransitions);

    public void RecordPressureConvergenceRecovery() =>
        Interlocked.Increment(ref _pressureConvergenceRecoveries);

    public void RecordStickyPressureRecovery() =>
        Interlocked.Increment(ref _stickyPressureRecoveries);

    public void RecordStabilizationWindowReset() =>
        Interlocked.Increment(ref _stabilizationWindowResets);

    public void RecordAdaptiveTtlRecovery() =>
        Interlocked.Increment(ref _adaptiveTtlRecoveries);

    public void RecordGovernanceFailsafeActivation() =>
        Interlocked.Increment(ref _governanceFailsafeActivations);

    public void RecordRuntimeBudgetConstrainedEvent() =>
        Interlocked.Increment(ref _runtimeBudgetConstrainedEvents);

    public void RecordProjectionComplexityElevation() =>
        Interlocked.Increment(ref _projectionComplexityElevations);

    public void RecordTelemetrySaturationEvent() =>
        Interlocked.Increment(ref _telemetrySaturationEvents);

    public void RecordExplainabilityTruncation() =>
        Interlocked.Increment(ref _explainabilityTruncations);

    public void RecordGovernanceSnapshotBuild() =>
        Interlocked.Increment(ref _governanceSnapshotBuilds);

    public void RecordGovernanceSnapshotReuse() =>
        Interlocked.Increment(ref _governanceSnapshotReuses);

    public void RecordProjectionReuseHit() =>
        Interlocked.Increment(ref _projectionReuseHits);

    public void RecordProjectionReuseMiss() =>
        Interlocked.Increment(ref _projectionReuseMisses);

    public void RecordSnapshotConsistencyTransition() =>
        Interlocked.Increment(ref _snapshotConsistencyTransitions);

    public void RecordGovernanceFingerprintTransition() =>
        Interlocked.Increment(ref _governanceFingerprintTransitions);

    public void RecordGovernanceStableFingerprintHit() =>
        Interlocked.Increment(ref _governanceStableFingerprintHits);

    public void RecordGovernanceDriftEscalation() =>
        Interlocked.Increment(ref _governanceDriftEscalations);

    public void RecordReplayConsistencyCheck() =>
        Interlocked.Increment(ref _replayConsistencyChecks);

    public void RecordProjectionFragmentationSignal() =>
        Interlocked.Increment(ref _projectionFragmentationSignals);

    public void RecordCompositionReuseHit() =>
        Interlocked.Increment(ref _compositionReuseHits);

    public void RecordCompositionReuseMiss() =>
        Interlocked.Increment(ref _compositionReuseMisses);

    public void RecordCompositionNestedReadAvoidance() =>
        Interlocked.Increment(ref _compositionNestedReadAvoidanceCount);

    public void RecordCompositionSnapshotBuild() =>
        Interlocked.Increment(ref _compositionSnapshotBuilds);

    public void ResetGovernanceStabilizationBaseline()
    {
        Interlocked.Exchange(ref _totalBypasses, 0);
        Interlocked.Exchange(ref _adaptiveTtlReductions, 0);
        Interlocked.Exchange(ref _repeatedColdMisses, 0);
        _repeatedColdMissesByCategory.Clear();
        _recentMissStreakByCategory.Clear();

        foreach (var counters in _byCategory.Values)
            Interlocked.Exchange(ref counters.Bypasses, 0);
    }

    public OperationalDiagnosticsCacheTelemetrySnapshotDto GetSnapshot()
    {
        var byCategory = _byCategory.ToDictionary(
            kvp => kvp.Key,
            kvp => new OperationalDiagnosticsCacheCategoryTelemetryDto
            {
                Category = kvp.Key,
                Hits = Volatile.Read(ref kvp.Value.Hits),
                Misses = Volatile.Read(ref kvp.Value.Misses),
                Bypasses = Volatile.Read(ref kvp.Value.Bypasses),
                StaleServes = Volatile.Read(ref kvp.Value.StaleServes)
            },
            StringComparer.Ordinal);

        return new OperationalDiagnosticsCacheTelemetrySnapshotDto
        {
            SnapshotUtc = DateTime.UtcNow,
            TotalHits = Volatile.Read(ref _totalHits),
            TotalMisses = Volatile.Read(ref _totalMisses),
            TotalBypasses = Volatile.Read(ref _totalBypasses),
            TotalStaleServes = Volatile.Read(ref _totalStaleServes),
            TotalInvalidations = Volatile.Read(ref _totalInvalidations),
            WarmRecommendations = Volatile.Read(ref _warmRecommendations),
            RepeatedColdMisses = Volatile.Read(ref _repeatedColdMisses),
            AdaptiveTtlReductions = Volatile.Read(ref _adaptiveTtlReductions),
            ScopedInvalidations = Volatile.Read(ref _scopedInvalidations),
            CrossCategoryInvalidations = Volatile.Read(ref _crossCategoryInvalidations),
            ScopedInvalidationRecoveries = Volatile.Read(ref _scopedInvalidationRecoveries),
            FreshnessRecoveryCount = Volatile.Read(ref _freshnessRecoveryCount),
            InvalidationDriftCount = Volatile.Read(ref _invalidationDriftCount),
            InvalidationPressureEscalations = Volatile.Read(ref _invalidationPressureEscalations),
            ConsistencyRecoveryCycles = Volatile.Read(ref _consistencyRecoveryCycles),
            ContainmentEscalations = Volatile.Read(ref _containmentEscalations),
            PropagationDetections = Volatile.Read(ref _propagationDetections),
            RecoveryWindowExtensions = Volatile.Read(ref _recoveryWindowExtensions),
            ConsistencyConfidenceDrops = Volatile.Read(ref _consistencyConfidenceDrops),
            PressureRecoveryCycles = Volatile.Read(ref _pressureRecoveryCycles),
            PressureLifecycleTransitions = Volatile.Read(ref _pressureLifecycleTransitions),
            PressureConvergenceRecoveries = Volatile.Read(ref _pressureConvergenceRecoveries),
            StickyPressureRecoveries = Volatile.Read(ref _stickyPressureRecoveries),
            StabilizationWindowResets = Volatile.Read(ref _stabilizationWindowResets),
            AdaptiveTtlRecoveries = Volatile.Read(ref _adaptiveTtlRecoveries),
            GovernanceFailsafeActivations = Volatile.Read(ref _governanceFailsafeActivations),
            RuntimeBudgetConstrainedEvents = Volatile.Read(ref _runtimeBudgetConstrainedEvents),
            ProjectionComplexityElevations = Volatile.Read(ref _projectionComplexityElevations),
            TelemetrySaturationEvents = Volatile.Read(ref _telemetrySaturationEvents),
            ExplainabilityTruncations = Volatile.Read(ref _explainabilityTruncations),
            GovernanceSnapshotBuilds = Volatile.Read(ref _governanceSnapshotBuilds),
            GovernanceSnapshotReuses = Volatile.Read(ref _governanceSnapshotReuses),
            ProjectionReuseHits = Volatile.Read(ref _projectionReuseHits),
            ProjectionReuseMisses = Volatile.Read(ref _projectionReuseMisses),
            SnapshotConsistencyTransitions = Volatile.Read(ref _snapshotConsistencyTransitions),
            GovernanceFingerprintTransitions = Volatile.Read(ref _governanceFingerprintTransitions),
            GovernanceStableFingerprintHits = Volatile.Read(ref _governanceStableFingerprintHits),
            GovernanceDriftEscalations = Volatile.Read(ref _governanceDriftEscalations),
            ReplayConsistencyChecks = Volatile.Read(ref _replayConsistencyChecks),
            ProjectionFragmentationSignals = Volatile.Read(ref _projectionFragmentationSignals),
            CompositionReuseHits = Volatile.Read(ref _compositionReuseHits),
            CompositionReuseMisses = Volatile.Read(ref _compositionReuseMisses),
            CompositionNestedReadAvoidanceCount = Volatile.Read(ref _compositionNestedReadAvoidanceCount),
            CompositionSnapshotBuilds = Volatile.Read(ref _compositionSnapshotBuilds),
            LastInvalidationUtc = _lastInvalidationUtc,
            ByCategory = byCategory,
            RepeatedColdMissesByCategory = _repeatedColdMissesByCategory.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value,
                StringComparer.Ordinal)
        };
    }

    private CategoryCounters GetCategory(string category) =>
        _byCategory.GetOrAdd(category, static c => new CategoryCounters(c));

    private sealed class CategoryCounters
    {
        public long Hits;
        public long Misses;
        public long Bypasses;
        public long StaleServes;

        public CategoryCounters(string category) => Category = category;

        public string Category { get; }

        public void IncrementHit() => Interlocked.Increment(ref Hits);
        public void IncrementMiss() => Interlocked.Increment(ref Misses);
        public void IncrementBypass() => Interlocked.Increment(ref Bypasses);
        public void IncrementStaleServe() => Interlocked.Increment(ref StaleServes);
    }
}
