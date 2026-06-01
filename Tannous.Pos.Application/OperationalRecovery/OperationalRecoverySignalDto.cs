namespace Tannous.Pos.Application.OperationalRecovery;

/// <summary>Bounded recovery posture signal for operator review.</summary>
public sealed class OperationalRecoverySignalDto
{
    public string SignalId { get; init; } = string.Empty;
    public string Domain { get; init; } = string.Empty;
    public OperationalRecoveryState State { get; init; }
    public OperationalRecoveryDirection Direction { get; init; }
    public OperationalRecoveryConfidence Confidence { get; init; }
    public OperationalRecoverySeverity Severity { get; init; }
    public string Summary { get; init; } = string.Empty;
    public string RecommendedRoute { get; init; } = string.Empty;
}
