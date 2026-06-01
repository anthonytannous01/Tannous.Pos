namespace Tannous.Pos.Application.Audit;

public sealed class OperationalCacheFreshnessRecoveryDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public string RecoveryState { get; init; } = string.Empty;
    public long FreshnessRecoveryCount { get; init; }
    public long TotalInvalidations { get; init; }
    public int AgingEntryCount { get; init; }
    public int NearExpiryEntryCount { get; init; }
    public int ExpiredEntryCount { get; init; }
    public int ActiveEntryCount { get; init; }
    public double InvalidationChurnRatio { get; init; }
    public IReadOnlyList<string> ReasonCodes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> TriggerSignals { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> GovernanceNotes { get; init; } = Array.Empty<string>();
}
