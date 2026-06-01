namespace Tannous.Pos.Application.OperationalInventoryWorkbench;

/// <summary>Top inventory drift pressure hotspot (category/source; advisory only).</summary>
public sealed class OperationalInventoryDriftHotspotDto
{
    public string Category { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public OperationalInventoryDriftSeverity Severity { get; init; }
    public int PressureCount { get; init; }
    public string Summary { get; init; } = string.Empty;
}
