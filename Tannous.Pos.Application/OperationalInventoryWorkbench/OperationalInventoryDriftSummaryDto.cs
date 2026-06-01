namespace Tannous.Pos.Application.OperationalInventoryWorkbench;

/// <summary>Operator-facing inventory drift summary (advisory counts only).</summary>
public sealed class OperationalInventoryDriftSummaryDto
{
    public int TotalInventoryDriftConflicts { get; init; }
    public int UnresolvedDriftConflicts { get; init; }
    public int EscalatingDriftConflicts { get; init; }
    public int ReplayLinkedDriftPressure { get; init; }
    public bool ProtectiveModeActive { get; init; }
    public OperationalInventoryDriftSeverity DriftSeverity { get; init; }
    public string Summary { get; init; } = string.Empty;
}
