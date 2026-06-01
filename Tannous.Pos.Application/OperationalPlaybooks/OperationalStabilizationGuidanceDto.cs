namespace Tannous.Pos.Application.OperationalPlaybooks;

/// <summary>Platform-wide stabilization sequencing guidance.</summary>
public sealed class OperationalStabilizationGuidanceDto
{
    public string DominantConstraint { get; init; } = string.Empty;
    public IReadOnlyList<string> RecommendedRecoveryOrder { get; init; } = Array.Empty<string>();
    public OperationalStabilizationPriority RecoveryAccelerationPotential { get; init; }
    public string OperationalRiskReduction { get; init; } = string.Empty;
    public OperationalGuidanceSeverity StabilizationLikelihood { get; init; }
    public string OperatorPriority { get; init; } = string.Empty;
}
