namespace Tannous.Pos.Application.OperationalConvergence;

/// <summary>Bounded overall operational convergence state (operator wording).</summary>
public enum OperationalConvergenceState
{
    Converged,
    MostlyConverged,
    Diverging,
    Ambiguous,
    Fragmented
}
