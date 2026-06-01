namespace Tannous.Pos.Application.Audit;

using Tannous.Pos.Application.Audit.Governance;

/// <summary>Consolidated operator-oriented cache governance snapshot (metadata/telemetry only).</summary>
public sealed class OperationalCacheGovernanceOverviewDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public OperationalCacheReadinessState ReadinessState { get; init; }
    public OperationalCachePressureSeverity PressureSeverity { get; init; }
    public OperationalCacheDegradationState DegradationState { get; init; }
    public OperationalCacheCardinalityClassification CardinalityClassification { get; init; }
    public OperationalCacheTtlMode DominantTtlMode { get; init; }
    public int StabilityScore { get; init; }
    public string StabilityClassification { get; init; } = string.Empty;
    public double HitRatio { get; init; }
    public long TotalHits { get; init; }
    public long TotalMisses { get; init; }
    public long TotalBypasses { get; init; }
    public long TotalInvalidations { get; init; }
    public int ActiveEntryCount { get; init; }
    public int ActiveScopedKeyCount { get; init; }
    public int WarmCandidateCount { get; init; }
    public bool WarmRecommendationsSuppressed { get; init; }
    public int AgingEntryCount { get; init; }
    public int NearExpiryEntryCount { get; init; }
    public int ExpiredEntryCount { get; init; }
    public OperationalCacheCardinalitySnapshotDto Cardinality { get; init; } = new();
    public OperationalCacheScopeDiagnosticsDto ScopeDiagnostics { get; init; } = new();
    public OperationalCacheDegradationDto Degradation { get; init; } = new();
    public string GovernanceNote { get; init; } = string.Empty;
    public IReadOnlyList<string> ReasonCodes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> TriggerSignals { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> GovernanceNotes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> RecommendedActions { get; init; } = Array.Empty<string>();
    public OperationalGovernanceProductionReadinessDto ProductionReadiness { get; init; } = new();
    public double CompositionReuseRatio { get; init; }
    public long NestedCompositionAvoidance { get; init; }
}
