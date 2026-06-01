namespace Tannous.Pos.Application.OperationalCausality;

/// <summary>Node within a bounded causal chain (advisory heuristic).</summary>
public sealed class OperationalCausalNodeDto
{
    public string ChainId { get; init; } = string.Empty;
    public string Area { get; init; } = string.Empty;
    public OperationalCausalRole Role { get; init; }
    public OperationalCausalitySeverity Severity { get; init; }
    public OperationalCausalityDirection Direction { get; init; }
    public bool IsUpstream { get; init; }
    public bool IsDownstream { get; init; }
    public bool IsStabilizing { get; init; }
    public string ContributionSummary { get; init; } = string.Empty;
}
