namespace Tannous.Pos.Application.OperationalStrategy;

/// <summary>Bounded strategic continuity across snapshot window.</summary>
public sealed class OperationalStrategyContinuityDto
{
    public string DominantStrategicShift { get; init; } = string.Empty;
    public string CoordinationConsistency { get; init; } = string.Empty;
    public string RecoveryStrategyAlignment { get; init; } = string.Empty;
    public string EscalationStrategyAlignment { get; init; } = string.Empty;
    public string StabilizationStrategyAlignment { get; init; } = string.Empty;

    /// <summary>Operator-readable oscillation assessment over bounded snapshot window.</summary>
    public string PostureOscillation { get; init; } = string.Empty;

    /// <summary>True when posture cycles between 2-3 values without resolution across the bounded window.</summary>
    public bool OscillationDetected { get; init; }

    public string OperatorInterpretation { get; init; } = string.Empty;
}
