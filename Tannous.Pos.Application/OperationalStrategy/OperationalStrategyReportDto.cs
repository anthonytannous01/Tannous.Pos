namespace Tannous.Pos.Application.OperationalStrategy;

/// <summary>Deterministic operational strategic posture report.</summary>
public sealed class OperationalStrategyReportDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public OperationalStrategicPostureType DominantOperationalPosture { get; init; }
    public string StrategicStabilizationState { get; init; } = string.Empty;
    public string EscalationCoordinationState { get; init; } = string.Empty;
    public string RecoveryCoordinationState { get; init; } = string.Empty;
    public OperationalCoordinationStrength OperationalAlignmentStrength { get; init; }
    public string DominantStrategicFocus { get; init; } = string.Empty;
    public IReadOnlyList<OperationalStrategicPostureDto> StrategicPostures { get; init; } =
        Array.Empty<OperationalStrategicPostureDto>();
    public IReadOnlyList<OperationalCoordinationDto> OperationalCoordination { get; init; } =
        Array.Empty<OperationalCoordinationDto>();
    public IReadOnlyList<OperationalStrategicAlignmentDto> StrategicAlignments { get; init; } =
        Array.Empty<OperationalStrategicAlignmentDto>();
    public OperationalStrategyContinuityDto StrategyContinuity { get; init; } = new();
    public string OperatorSummary { get; init; } = string.Empty;
    public string StrategyNote { get; init; } =
        "Advisory deterministic operational strategic posture from bounded cognition continuity. Not business intelligence, executive planning, or AI recommendations.";
}
