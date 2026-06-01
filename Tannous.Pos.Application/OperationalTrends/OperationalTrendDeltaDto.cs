namespace Tannous.Pos.Application.OperationalTrends;

/// <summary>Bounded delta between current operational state and a prior snapshot.</summary>
public sealed class OperationalTrendDeltaDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public DateTime ComparedToUtc { get; init; }
    public OperationalTrendDirection OverallDirection { get; init; }
    public OperationalTrendSeverity Severity { get; init; }
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyList<string> MovementSignals { get; init; } = Array.Empty<string>();
}
