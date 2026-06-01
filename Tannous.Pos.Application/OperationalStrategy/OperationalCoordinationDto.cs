namespace Tannous.Pos.Application.OperationalStrategy;

/// <summary>Deterministic operational coordination signal for strategic posture.</summary>
public sealed class OperationalCoordinationDto
{
    public string CoordinationId { get; init; } = string.Empty;
    public string DominantOperationalStrategy { get; init; } = string.Empty;
    public OperationalCoordinationStrength CoordinationStrength { get; init; }
    public string StabilizationCoordination { get; init; } = string.Empty;
    public string EscalationCoordination { get; init; } = string.Empty;
    public string RecoveryCoordination { get; init; } = string.Empty;
    public string OperatorSummary { get; init; } = string.Empty;
}
