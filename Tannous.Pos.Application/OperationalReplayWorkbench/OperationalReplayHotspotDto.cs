namespace Tannous.Pos.Application.OperationalReplayWorkbench;

/// <summary>Top replay pressure hotspot (advisory only).</summary>
public sealed class OperationalReplayHotspotDto
{
    public string Category { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public OperationalReplayPressureLevel Severity { get; init; }
    public int PressureCount { get; init; }
    public string Summary { get; init; } = string.Empty;
}
