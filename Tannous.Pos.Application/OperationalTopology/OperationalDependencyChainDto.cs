namespace Tannous.Pos.Application.OperationalTopology;

/// <summary>Bounded deterministic operational dependency chain.</summary>
public sealed class OperationalDependencyChainDto
{
    public string ChainId { get; init; } = string.Empty;
    public string DominantOperationalFlow { get; init; } = string.Empty;
    public string UpstreamArea { get; init; } = string.Empty;
    public string DownstreamArea { get; init; } = string.Empty;
    public IReadOnlyList<string> DependencySequence { get; init; } = Array.Empty<string>();
    public string EscalationRisk { get; init; } = string.Empty;
    public string StabilizationPotential { get; init; } = string.Empty;
    public string OperatorSummary { get; init; } = string.Empty;
}
