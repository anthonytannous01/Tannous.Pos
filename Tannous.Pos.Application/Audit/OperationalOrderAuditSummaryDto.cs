namespace Tannous.Pos.Application.Audit;

/// <summary>Aggregated audit health summary for a specific order (counts only — not records).</summary>
public sealed class OperationalOrderAuditSummaryDto
{
    public Guid OrderId { get; init; }
    public int AuditRecordCount { get; init; }
    public string HighestSeverity { get; init; } = OperationalAuditSeverity.Information;
    public int UnresolvedConflictCount { get; init; }
    public DateTime? LastActivityUtc { get; init; }
}
