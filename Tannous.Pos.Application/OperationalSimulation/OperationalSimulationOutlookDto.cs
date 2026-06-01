using Tannous.Pos.Application.OperationalRecovery;

namespace Tannous.Pos.Application.OperationalSimulation;

/// <summary>Platform-wide hypothetical simulation outlook.</summary>
public sealed class OperationalSimulationOutlookDto
{
    public string MostLikelyStabilizationPath { get; init; } = string.Empty;
    public string HighestRiskDegradationPath { get; init; } = string.Empty;
    public string DominantConstraint { get; init; } = string.Empty;
    public string StrongestLeveragePoint { get; init; } = string.Empty;
    public OperationalSimulationDirection PlatformRecoveryTrajectory { get; init; }
    public OperationalRecoveryConfidence OperationalConfidence { get; init; }
}
