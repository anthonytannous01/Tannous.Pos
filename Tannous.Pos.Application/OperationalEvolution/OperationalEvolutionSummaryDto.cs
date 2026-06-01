namespace Tannous.Pos.Application.OperationalEvolution;

/// <summary>Compact operational evolution summary.</summary>
public sealed class OperationalEvolutionSummaryDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public string DominantTransition { get; init; } = string.Empty;
    public string RecoveryDirection { get; init; } = string.Empty;
    public string EscalationDirection { get; init; } = string.Empty;
    public string StabilizationDirection { get; init; } = string.Empty;
    public OperationalMomentumState OperationalMomentumState { get; init; }
    public string OperatorAttentionLevel { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string EvolutionNote { get; init; } =
        "Advisory deterministic evolution summary from bounded process-local continuity.";
}
