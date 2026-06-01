namespace Tannous.Pos.Application.Audit;

/// <summary>Computed governance audit projection (metadata/telemetry only; not persisted).</summary>
public sealed class OperationalCacheGovernanceAuditDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public bool CacheModeConsistent { get; init; }
    public bool AdaptiveTtlAligned { get; init; }
    public bool PressureDegradationAligned { get; init; }
    public bool InvalidationSurvivabilityHealthy { get; init; }
    public bool ReadinessDegradationCoherent { get; init; }
    public bool ScopedKeySaturationAligned { get; init; }
    public string DominantTtlMode { get; init; } = string.Empty;
    public string PressureSeverity { get; init; } = string.Empty;
    public string DegradationState { get; init; } = string.Empty;
    public string ReadinessState { get; init; } = string.Empty;
    public string CardinalityClassification { get; init; } = string.Empty;
    public int StabilityScore { get; init; }
    public int SurvivabilityScore { get; init; }
    public string SurvivabilityClassification { get; init; } = string.Empty;
    public int AgingEntryCount { get; init; }
    public int NearExpiryEntryCount { get; init; }
    public int ExpiredEntryCount { get; init; }
    public OperationalCacheGovernanceDriftDto Drift { get; init; } = new();
    public OperationalCacheGovernanceConsistencyDto Consistency { get; init; } = new();
    public IReadOnlyList<OperationalCacheGovernanceRecommendationDto> Recommendations { get; init; } =
        Array.Empty<OperationalCacheGovernanceRecommendationDto>();
    public IReadOnlyList<string> ReasonCodes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> TriggerSignals { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> GovernanceNotes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> RecommendedActions { get; init; } = Array.Empty<string>();
}
