namespace Tannous.Pos.Application.OperationalReconciliation;

/// <summary>Raw query result for reconciliation system state — used by OperationalReconciliationSystemService only.</summary>
public sealed class OperationalReconciliationAuditSummaryDto
{
    public int TotalUnresolvedConflicts { get; init; }
    public DateTime? OldestUnresolvedConflictUtc { get; init; }
    public int OrderScopedUnresolvedConflicts { get; init; }
}
