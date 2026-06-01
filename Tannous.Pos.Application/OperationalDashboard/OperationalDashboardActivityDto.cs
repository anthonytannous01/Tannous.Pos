namespace Tannous.Pos.Application.OperationalDashboard;

/// <summary>Aggregate operational activity counts for operator review.</summary>
public sealed class OperationalDashboardActivityDto
{
    public int ActiveAlertCount { get; init; }
    public int UnresolvedReconciliationCount { get; init; }
    public int InvestigatingReconciliationCount { get; init; }
    public int IncidentGroupCount { get; init; }
    public int ReplayMismatchCount { get; init; }
    public int InventoryDriftRiskCount { get; init; }
    public string Summary { get; init; } = string.Empty;
}
