namespace Tannous.Pos.Application.OperationalEvolution;

/// <summary>Cross-snapshot operational evolution continuity narrative.</summary>
public sealed class OperationalEvolutionContinuityDto
{
    public string DominantNarrativeTransition { get; init; } = string.Empty;
    public string RepeatingOperationalFlow { get; init; } = string.Empty;
    public string StabilizationConsistency { get; init; } = string.Empty;
    public string EscalationConsistency { get; init; } = string.Empty;
    public string RecoveryConsistency { get; init; } = string.Empty;
    public string OperatorInterpretation { get; init; } = string.Empty;
}
