namespace Tannous.Pos.Application.OperationalTrends;

/// <summary>
/// Bounded process-local trend capture (classifications and counts only).
/// NON-GOAL: no payloads, entity IDs, receipts, exports, or cache metadata.
/// </summary>
public sealed class OperationalTrendSnapshot
{
    public DateTime CapturedAtUtc { get; init; }
    public string FingerprintId { get; init; } = string.Empty;
    public string FingerprintStability { get; init; } = string.Empty;
    public string ReadinessState { get; init; } = string.Empty;
    public string PressureBand { get; init; } = string.Empty;
    public string HealthState { get; init; } = string.Empty;
    public int UnresolvedReconciliationCount { get; init; }
    public int InventoryDriftConflictCount { get; init; }
    public int ActiveReplayPressure { get; init; }
    public string ReplayInstabilityLevel { get; init; } = string.Empty;
    public bool ProtectiveModeActive { get; init; }
    public int ActiveAlertCount { get; init; }
    public bool ReplayStabilizationActive { get; init; }
    public int EscalatingConflictCount { get; init; }
}
