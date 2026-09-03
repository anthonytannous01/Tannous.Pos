namespace Tannous.Pos.Application.OperationalWorkbench;

/// <summary>Operator-facing reconciliation queue visibility (advisory counts only).</summary>
public sealed class OperationalReconciliationQueueDto
{
    public int ActiveConflicts { get; init; }
    public int UnresolvedConflicts { get; init; }
    public int ReplayRiskConflicts { get; init; }
    public int InventoryDriftConflicts { get; init; }
    public int EscalatingConflicts { get; init; }
    public string Summary { get; init; } = string.Empty;
}
