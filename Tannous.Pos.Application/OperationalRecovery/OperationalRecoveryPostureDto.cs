namespace Tannous.Pos.Application.OperationalRecovery;

/// <summary>Operator-facing operational recovery posture summary.</summary>
public sealed class OperationalRecoveryPostureDto
{
    public OperationalRecoveryState OverallState { get; init; }
    public OperationalRecoveryDirection OverallDirection { get; init; }
    public OperationalRecoveryConfidence OverallConfidence { get; init; }
    public OperationalRecoverySeverity OverallSeverity { get; init; }
    public string Summary { get; init; } = string.Empty;
    public int SignalCount { get; init; }
    public int RecommendationCount { get; init; }
    public int AttentionCount { get; init; }
    public IReadOnlyList<OperationalRecoverySignalDto> Signals { get; init; } = Array.Empty<OperationalRecoverySignalDto>();
    public IReadOnlyList<OperationalRecoveryConvergenceDto> Convergence { get; init; } = Array.Empty<OperationalRecoveryConvergenceDto>();
    public IReadOnlyList<OperationalRecoveryAttentionDto> Attention { get; init; } = Array.Empty<OperationalRecoveryAttentionDto>();
    public IReadOnlyList<OperationalRecoveryRecommendationDto> Recommendations { get; init; } = Array.Empty<OperationalRecoveryRecommendationDto>();
}
