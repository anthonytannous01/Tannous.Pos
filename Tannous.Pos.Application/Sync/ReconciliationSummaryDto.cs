namespace Tannous.Pos.Application.Sync;

public sealed class ReconciliationSummaryDto
{
    public int UnresolvedCount { get; init; }
    public int InvestigatingCount { get; init; }
    public int ResolvedCount { get; init; }
    public int ReplayMismatchCount { get; init; }
    public int ConcurrencyConflictCount { get; init; }
    public int LifecycleConflictCount { get; init; }
    public int InventoryDriftRiskCount { get; init; }
}
