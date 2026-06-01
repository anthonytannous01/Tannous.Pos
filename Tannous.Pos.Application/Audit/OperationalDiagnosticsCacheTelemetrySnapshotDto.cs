namespace Tannous.Pos.Application.Audit;

public class OperationalDiagnosticsCacheTelemetrySnapshotDto
{
    public long TotalHits { get; set; }
    public long TotalMisses { get; set; }
    public long TotalBypasses { get; set; }
    public long TotalStaleServes { get; set; }
    public long TotalInvalidations { get; set; }
    public long WarmRecommendations { get; set; }
    public long RepeatedColdMisses { get; set; }
    public long AdaptiveTtlReductions { get; set; }
    public long ScopedInvalidations { get; set; }
    public long CrossCategoryInvalidations { get; set; }
    public long ScopedInvalidationRecoveries { get; set; }
    public long FreshnessRecoveryCount { get; set; }
    public long InvalidationDriftCount { get; set; }
    public long InvalidationPressureEscalations { get; set; }
    public long ConsistencyRecoveryCycles { get; set; }
    public long ContainmentEscalations { get; set; }
    public long PropagationDetections { get; set; }
    public long RecoveryWindowExtensions { get; set; }
    public long ConsistencyConfidenceDrops { get; set; }
    public long PressureRecoveryCycles { get; set; }
    public long PressureLifecycleTransitions { get; set; }
    public long PressureConvergenceRecoveries { get; set; }
    public long StickyPressureRecoveries { get; set; }
    public long StabilizationWindowResets { get; set; }
    public long AdaptiveTtlRecoveries { get; set; }
    public long GovernanceFailsafeActivations { get; set; }
    public long RuntimeBudgetConstrainedEvents { get; set; }
    public long ProjectionComplexityElevations { get; set; }
    public long TelemetrySaturationEvents { get; set; }
    public long ExplainabilityTruncations { get; set; }
    public long GovernanceSnapshotBuilds { get; set; }
    public long GovernanceSnapshotReuses { get; set; }
    public long ProjectionReuseHits { get; set; }
    public long ProjectionReuseMisses { get; set; }
    public long SnapshotConsistencyTransitions { get; set; }
    public long GovernanceFingerprintTransitions { get; set; }
    public long GovernanceStableFingerprintHits { get; set; }
    public long GovernanceDriftEscalations { get; set; }
    public long ReplayConsistencyChecks { get; set; }
    public long ProjectionFragmentationSignals { get; set; }
    public long CompositionReuseHits { get; set; }
    public long CompositionReuseMisses { get; set; }
    public long CompositionNestedReadAvoidanceCount { get; set; }
    public long CompositionSnapshotBuilds { get; set; }
    public DateTime? LastInvalidationUtc { get; set; }
    public DateTime SnapshotUtc { get; set; }
    public IReadOnlyDictionary<string, OperationalDiagnosticsCacheCategoryTelemetryDto> ByCategory { get; set; }
        = new Dictionary<string, OperationalDiagnosticsCacheCategoryTelemetryDto>();
    public IReadOnlyDictionary<string, long> RepeatedColdMissesByCategory { get; set; }
        = new Dictionary<string, long>(StringComparer.Ordinal);
}

public class OperationalDiagnosticsCacheCategoryTelemetryDto
{
    public string Category { get; set; } = string.Empty;
    public long Hits { get; set; }
    public long Misses { get; set; }
    public long Bypasses { get; set; }
    public long StaleServes { get; set; }
}
