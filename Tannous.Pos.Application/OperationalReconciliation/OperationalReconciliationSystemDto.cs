using Tannous.Pos.Application.OperationalBriefing;

namespace Tannous.Pos.Application.OperationalReconciliation;

/// <summary>
/// System-level, non-paginated view of the reconciliation subsystem health.
/// Reports total unresolved conflict backlog, oldest conflict age, and entity-type breakdown.
/// Not a substitute for GET /conflicts/recent (paginated record detail) — provides aggregate health metrics only.
/// Advisory and read-only.
/// </summary>
public sealed class OperationalReconciliationSystemDto
{
    public DateTime AssessedAtUtc { get; init; }
    public int TotalUnresolvedConflicts { get; init; }
    public DateTime? OldestUnresolvedConflictUtc { get; init; }
    public int OrderScopedUnresolvedConflicts { get; init; }
    public int OtherEntityUnresolvedConflicts { get; init; }
    public ReconciliationSystemHealth SystemHealth { get; init; }
    public string SystemHealthNarrative { get; init; } = string.Empty;
    public BriefingCognitionAge SystemCognitionAge { get; init; }
    public string SystemContextSummary { get; init; } = string.Empty;
}
