namespace Tannous.Pos.Application.Audit;

public sealed class OperationalCacheConsistencyConfidenceDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public string ConfidenceLevel { get; init; } = string.Empty;
    public int ConfidenceScore { get; init; }
    public long ConsistencyConfidenceDrops { get; init; }
    public double HitRatio { get; init; }
    public double BypassRatio { get; init; }
    public double StaleServeRatio { get; init; }
    public int StabilityScore { get; init; }
    public string SurvivabilityClassification { get; init; } = string.Empty;
    public OperationalCacheRecoveryWindowDto RecoveryWindow { get; init; } = new();
    public IReadOnlyList<string> ReasonCodes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> TriggerSignals { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> GovernanceNotes { get; init; } = Array.Empty<string>();
}
