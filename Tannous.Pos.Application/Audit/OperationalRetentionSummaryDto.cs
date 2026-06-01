namespace Tannous.Pos.Application.Audit;

/// <summary>Safe aggregated operational lifecycle metrics for internal Admin retention diagnostics.</summary>
public sealed class OperationalRetentionSummaryDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public int UnresolvedConflictCount { get; init; }
    public int UnresolvedOver7DaysCount { get; init; }
    public int UnresolvedOver30DaysCount { get; init; }
    public int AuditRecordCount { get; init; }
    public int SyncConflictRecordCount { get; init; }
    public int ReplayReceiptCount { get; init; }
    public int ReplayMismatchUnresolvedCount { get; init; }
    public int InventoryDriftUnresolvedCount { get; init; }
    public DateTime? OldestUnresolvedConflictUtc { get; init; }
    public bool TruncationWarningsIndicated { get; init; }
    public string PrimaryDegradedMode { get; init; } = OperationalDegradedModeTypes.Normal;
    public bool QueryPressureIndicated { get; init; }
    public bool ReplayStormRiskIndicated { get; init; }
    public bool ExportTruncationPressureIndicated { get; init; }
    public string ReconciliationBacklogSeverity { get; init; } = "Normal";
    public IReadOnlyDictionary<string, string> RetentionGuidance { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
