using Tannous.Pos.Application.OperationalSituationRoom;

namespace Tannous.Pos.Application.OperationalSimulation;

/// <summary>Platform-wide hypothetical simulation summary.</summary>
public sealed class OperationalSimulationSummaryDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public int ActiveSimulationCount { get; init; }
    public int StabilizationScenarioCount { get; init; }
    public int DegradationScenarioCount { get; init; }
    public string HighestLeverageArea { get; init; } = string.Empty;
    public string DominantOperationalConstraint { get; init; } = string.Empty;
    public OperationalLeverageStrength RecoveryAccelerationPotential { get; init; }
    public OperationalAttentionLevel OperatorAttentionLevel { get; init; }
    public string Summary { get; init; } = string.Empty;
    public string SimulationNote { get; init; } =
        "Advisory deterministic hypothetical analysis composed from existing diagnostics. Heuristic what-if only — not prediction, ML, or optimization.";
}
