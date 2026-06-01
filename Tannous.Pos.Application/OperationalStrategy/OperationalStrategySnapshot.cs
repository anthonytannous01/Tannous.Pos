namespace Tannous.Pos.Application.OperationalStrategy;

/// <summary>Lightweight strategy snapshot for bounded FIFO continuity.</summary>
public sealed class OperationalStrategySnapshot
{
    public DateTime GeneratedAtUtc { get; init; }
    public OperationalStrategicPostureType DominantOperationalPosture { get; init; }
    public OperationalCoordinationStrength OperationalAlignmentStrength { get; init; }
    public string DominantStrategicFocus { get; init; } = string.Empty;
    public int CoordinationCount { get; init; }
}
