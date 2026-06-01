using Tannous.Pos.Application.Audit;

namespace Tannous.Pos.Application.OperationalEntityStatus;

/// <summary>
/// Pre-correlated operational health assessment for a specific order.
/// Contains counts and classification — not raw audit records.
/// For record-level detail use GET /internal/operational-audit/timeline/order/{orderId}.
/// </summary>
public sealed class OperationalOrderStatusDto
{
    public Guid OrderId { get; init; }
    public DateTime AssessedAtUtc { get; init; }
    public int AuditRecordCount { get; init; }
    public string HighestSeverity { get; init; } = OperationalAuditSeverity.Information;
    public int UnresolvedConflictCount { get; init; }
    public DateTime? LastActivityUtc { get; init; }
    public EntityHealthClassification HealthClassification { get; init; }
    public string StatusNarrative { get; init; } = string.Empty;
}
