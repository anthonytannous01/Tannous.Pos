namespace Tannous.Pos.Application.OperationalDashboard;

/// <summary>Operator-facing operational health snapshot (advisory only).</summary>
public sealed class OperationalDashboardHealthDto
{
    public OperationalDashboardHealthState State { get; init; }
    public OperationalDashboardAttentionState AttentionState { get; init; }
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyList<string> HealthFactors { get; init; } = Array.Empty<string>();
}
