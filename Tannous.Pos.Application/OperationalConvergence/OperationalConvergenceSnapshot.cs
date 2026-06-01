namespace Tannous.Pos.Application.OperationalConvergence;

/// <summary>Lightweight process-local convergence snapshot for short-term continuity.</summary>
public sealed class OperationalConvergenceSnapshot
{
    public DateTime GeneratedAtUtc { get; init; }
    public OperationalConvergenceStrength ConvergenceStrength { get; init; }
    public OperationalConvergenceState ConvergenceState { get; init; }
    public string DominantOperationalNarrative { get; init; } = string.Empty;
    public string HighestAmbiguityArea { get; init; } = string.Empty;
    public int ReinforcementCount { get; init; }
    public int DivergenceCount { get; init; }
}
