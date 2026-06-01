namespace Tannous.Pos.Application.Audit;

/// <summary>Aggregated audit health summary for a specific device (counts only — not records).</summary>
public sealed class OperationalDeviceAuditSummaryDto
{
    public string DeviceId { get; init; } = string.Empty;
    public int AuditRecordCount { get; init; }
    public string HighestSeverity { get; init; } = OperationalAuditSeverity.Information;
    public int UnresolvedConflictCount { get; init; }
    public int ReceiptTotal { get; init; }
    public int ReceiptSuccessCount { get; init; }
    public int ReceiptConflictCount { get; init; }
    public DateTime? LastActivityUtc { get; init; }
}
