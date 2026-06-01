namespace Tannous.Pos.Application.OperationalTimeline;

/// <summary>Compact capture snapshot for transition detection (classifications/counts only).</summary>
public sealed class OperationalTimelineCaptureSnapshot
{
    public DateTime CapturedAtUtc { get; init; }
    public int ActiveReplayPressure { get; init; }
    public string ReplayInstabilityLevel { get; init; } = string.Empty;
    public bool ProtectiveModeActive { get; init; }
    public int InventoryDriftConflictCount { get; init; }
    public int UnresolvedReconciliationCount { get; init; }
    public int EscalatingConflictCount { get; init; }
    public bool ReplayStabilizationActive { get; init; }
    public bool ReplayRecoveryImproving { get; init; }
    public string TrendDirection { get; init; } = string.Empty;
    public string FingerprintStability { get; init; } = string.Empty;
    public bool FingerprintChanged { get; init; }
    public string HealthState { get; init; } = string.Empty;
}
