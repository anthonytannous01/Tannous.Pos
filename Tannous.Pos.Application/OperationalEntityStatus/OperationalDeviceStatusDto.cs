using Tannous.Pos.Application.Audit;

namespace Tannous.Pos.Application.OperationalEntityStatus;

/// <summary>
/// Pre-correlated operational health assessment for a specific device.
/// Contains counts and classification — not raw audit records or receipts.
/// For record-level detail use GET /internal/operational-audit/timeline/device/{deviceId}.
/// </summary>
public sealed class OperationalDeviceStatusDto
{
    public string DeviceId { get; init; } = string.Empty;
    public DateTime AssessedAtUtc { get; init; }
    public int AuditRecordCount { get; init; }
    public string HighestSeverity { get; init; } = OperationalAuditSeverity.Information;
    public int UnresolvedConflictCount { get; init; }
    public int ReceiptTotal { get; init; }
    public int ReceiptSuccessCount { get; init; }
    public int ReceiptConflictCount { get; init; }
    public DateTime? LastActivityUtc { get; init; }
    public EntityHealthClassification HealthClassification { get; init; }
    public string StatusNarrative { get; init; } = string.Empty;
}
