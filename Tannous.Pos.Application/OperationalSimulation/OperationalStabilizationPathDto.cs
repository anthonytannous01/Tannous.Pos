namespace Tannous.Pos.Application.OperationalSimulation;

/// <summary>Deterministic hypothetical stabilization path.</summary>
public sealed class OperationalStabilizationPathDto
{
    public string PathId { get; init; } = string.Empty;
    public string DominantArea { get; init; } = string.Empty;
    public IReadOnlyList<string> ExpectedImprovementSequence { get; init; } = Array.Empty<string>();
    public string BlockingConstraint { get; init; } = string.Empty;
    public OperationalLeverageStrength RecoveryAccelerationPotential { get; init; }
    public OperationalSimulationConfidence StabilizationConfidence { get; init; }
    public string EstimatedOperationalImpact { get; init; } = string.Empty;
    public string OperatorSummary { get; init; } = string.Empty;
}
