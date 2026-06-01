namespace Tannous.Pos.Application.Audit;

public sealed class OperationalCacheStabilityDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public int StabilityScore { get; init; }
    public string StabilityClassification { get; init; } = string.Empty;
    public string RecommendedOperatorAction { get; init; } = string.Empty;
    public double HitRatio { get; init; }
    public double StaleServeRatio { get; init; }
    public double BypassRatio { get; init; }
    public long InvalidationChurn { get; init; }
    public long RepeatedColdMisses { get; init; }
    public IReadOnlyList<string> ReasonCodes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> TriggerSignals { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> GovernanceNotes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> RecommendedActions { get; init; } = Array.Empty<string>();
}
