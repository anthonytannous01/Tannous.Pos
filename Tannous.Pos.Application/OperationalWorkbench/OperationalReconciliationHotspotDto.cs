namespace Tannous.Pos.Application.OperationalWorkbench;

/// <summary>Top reconciliation pressure hotspot (category/source; advisory only).</summary>
public sealed class OperationalReconciliationHotspotDto
{
    public string Category { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public OperationalWorkbenchSeverity Severity { get; init; }
    public int PressureCount { get; init; }
    public string Summary { get; init; } = string.Empty;
}
