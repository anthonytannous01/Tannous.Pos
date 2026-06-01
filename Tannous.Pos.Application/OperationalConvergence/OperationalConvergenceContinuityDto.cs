namespace Tannous.Pos.Application.OperationalConvergence;

/// <summary>Short-term convergence continuity from bounded snapshots.</summary>
public sealed class OperationalConvergenceContinuityDto
{
    public string DominantConvergenceShift { get; init; } = string.Empty;
    public string ReinforcementStability { get; init; } = string.Empty;
    public string DivergenceConsistency { get; init; } = string.Empty;
    public string RecoveryConvergenceAlignment { get; init; } = string.Empty;
    public string EscalationConvergenceAlignment { get; init; } = string.Empty;
    public string OperatorInterpretation { get; init; } = string.Empty;
}
