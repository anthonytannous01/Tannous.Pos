namespace Tannous.Pos.Application.OperationalStrategy;

/// <summary>Condensed operational strategic posture summary.</summary>
public sealed class OperationalStrategySummaryDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public OperationalStrategicPostureType DominantStrategicPosture { get; init; }
    public string StrongestOperationalAlignment { get; init; } = string.Empty;
    public string WeakestCoordinationArea { get; init; } = string.Empty;
    public string DominantStrategicPressure { get; init; } = string.Empty;
    public OperationalStrategyState OperationalStrategyState { get; init; }
    public string OperatorAttentionLevel { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
}
