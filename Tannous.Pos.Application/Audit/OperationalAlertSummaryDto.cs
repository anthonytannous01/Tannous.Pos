namespace Tannous.Pos.Application.Audit;

/// <summary>Aggregate counts for active alert signals (query-time; not persisted).</summary>
public sealed class OperationalAlertSummaryDto
{
    public int TotalSignals { get; init; }
    public int CriticalSignals { get; init; }
    public int WarningSignals { get; init; }
    public int ReplayRelatedSignals { get; init; }
    public int InventoryRelatedSignals { get; init; }
    public DateTime GeneratedAtUtc { get; init; }
    public string GovernanceNote { get; init; } = OperationalAlertGovernance.GetNonGoalsStatement();
}
