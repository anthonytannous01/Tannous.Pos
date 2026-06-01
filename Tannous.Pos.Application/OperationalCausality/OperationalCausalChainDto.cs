namespace Tannous.Pos.Application.OperationalCausality;

/// <summary>Bounded operator causal chain (advisory; process-local).</summary>
public sealed class OperationalCausalChainDto
{
    public string ChainId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string DominantArea { get; init; } = string.Empty;
    public string RootCauseCandidate { get; init; } = string.Empty;
    public string StabilizationBlocker { get; init; } = string.Empty;
    public OperationalCausalityDirection PropagationDirection { get; init; }
    public string RecoveryImpact { get; init; } = string.Empty;
    public OperationalCausalityConfidence OperationalConfidence { get; init; }
    public DateTime FirstObservedUtc { get; init; }
    public DateTime LastObservedUtc { get; init; }
    public int CorrelatedIncidentCount { get; init; }
    public int PropagationDepth { get; init; }
    public string OperatorSummary { get; init; } = string.Empty;
}
