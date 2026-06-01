namespace Tannous.Pos.Application.OperationalAttention;

/// <summary>Lightweight attention snapshot for bounded FIFO continuity.</summary>
public sealed class OperationalAttentionSnapshot
{
    public DateTime GeneratedAtUtc { get; init; }
    public OperationalPriorityType DominantOperationalPriority { get; init; }
    public OperationalUrgencyLevel AttentionPressureLevel { get; init; }
    public string HighestUrgencyArea { get; init; } = string.Empty;
    public int PriorityCount { get; init; }
}
