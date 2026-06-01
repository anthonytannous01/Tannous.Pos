namespace Tannous.Pos.Application.OperationalTopology;

/// <summary>Condensed operational topology summary for operator attention.</summary>
public sealed class OperationalTopologySummaryDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public string DominantDependencyFlow { get; init; } = string.Empty;
    public string HighestRiskDependency { get; init; } = string.Empty;
    public string StrongestStabilizationInfluence { get; init; } = string.Empty;
    public string StrongestEscalationInfluence { get; init; } = string.Empty;
    public OperationalTopologyState OperationalCriticalityState { get; init; }
    public string OperatorAttentionLevel { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
}
