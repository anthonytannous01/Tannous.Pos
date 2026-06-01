namespace Tannous.Pos.Application.OperationalDashboard;

/// <summary>Operator-facing risk summary derived from existing diagnostics.</summary>
public sealed class OperationalDashboardRiskDto
{
    public OperationalDashboardRiskLevel Level { get; init; }
    public string Summary { get; init; } = string.Empty;
    public int UnresolvedConflictCount { get; init; }
    public int CriticalAlertCount { get; init; }
    public int HighRiskIncidentCount { get; init; }
    public IReadOnlyList<string> PrimaryRisks { get; init; } = Array.Empty<string>();
}
