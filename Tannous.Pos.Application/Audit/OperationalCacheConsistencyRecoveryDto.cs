namespace Tannous.Pos.Application.Audit;

public sealed class OperationalCacheConsistencyRecoveryDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public string ContainmentState { get; init; } = string.Empty;
    public string ConfidenceLevel { get; init; } = string.Empty;
    public long ConsistencyRecoveryCycles { get; init; }
    public long RecoveryWindowExtensions { get; init; }
    public int ActiveEntryCount { get; init; }
    public int ExpiredEntryCount { get; init; }
    public double HitRatio { get; init; }
    public double InvalidationChurnRatio { get; init; }
    public OperationalCacheRecoveryWindowDto RecoveryWindow { get; init; } = new();
    public IReadOnlyList<string> ReasonCodes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> TriggerSignals { get; init; } = Array.Empty<string>();
    public IReadOnlyList<OperationalCacheContainmentRecommendationDto> Recommendations { get; init; } =
        Array.Empty<OperationalCacheContainmentRecommendationDto>();
    public IReadOnlyList<string> GovernanceNotes { get; init; } = Array.Empty<string>();
}
