namespace Tannous.Pos.Application.OperationalEvolution;

/// <summary>Lightweight process-local evolution snapshot for short-term continuity.</summary>
public sealed class OperationalEvolutionSnapshot
{
    public DateTime GeneratedAtUtc { get; init; }
    public OperationalEvolutionDirection DominantEvolutionDirection { get; init; }
    public string RecoveryMomentum { get; init; } = string.Empty;
    public string EscalationMomentum { get; init; } = string.Empty;
    public string StabilizationMomentum { get; init; } = string.Empty;
    public int ActiveTransitionCount { get; init; }
    public string DominantOperationalShift { get; init; } = string.Empty;
}
