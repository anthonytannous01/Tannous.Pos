namespace Tannous.Pos.Application.Audit;

public sealed class OperationalCacheRecoveryWindowDto
{
    public string WindowClassification { get; init; } = string.Empty;
    public bool StabilizationAchieved { get; init; }
    public bool ChurnReboundDetected { get; init; }
    public long RecoveryWindowExtensions { get; init; }
    public long ConsistencyRecoveryCycles { get; init; }
    public int ExpiredEntryCount { get; init; }
    public IReadOnlyList<string> StabilizationSignals { get; init; } = Array.Empty<string>();
}
