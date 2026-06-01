namespace Tannous.Pos.Application.OperationalStrategy;

/// <summary>Strategic posture for a bounded operational area.</summary>
public sealed class OperationalStrategicPostureDto
{
    public string OperationalArea { get; init; } = string.Empty;
    public OperationalStrategicDirection StrategicOrientation { get; init; }
    public string StabilizationAlignment { get; init; } = string.Empty;
    public string EscalationAlignment { get; init; } = string.Empty;
    public string RecoveryAlignment { get; init; } = string.Empty;
    public OperationalCoordinationStrength StrategicInfluenceStrength { get; init; }
    public string OperatorInterpretation { get; init; } = string.Empty;
}
