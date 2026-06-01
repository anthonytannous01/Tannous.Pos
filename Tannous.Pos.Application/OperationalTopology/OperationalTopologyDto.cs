namespace Tannous.Pos.Application.OperationalTopology;

/// <summary>Deterministic operational topology and dependency intelligence.</summary>
public sealed class OperationalTopologyDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public string DominantOperationalTopology { get; init; } = string.Empty;
    public string HighestInfluenceArea { get; init; } = string.Empty;
    public string HighestDependencyRisk { get; init; } = string.Empty;
    public string StabilizationDependencyStrength { get; init; } = string.Empty;
    public string EscalationPropagationStrength { get; init; } = string.Empty;
    public OperationalTopologyState TopologyState { get; init; }
    public IReadOnlyList<OperationalDependencyDto> Dependencies { get; init; } = Array.Empty<OperationalDependencyDto>();
    public IReadOnlyList<OperationalInfluenceDto> Influences { get; init; } = Array.Empty<OperationalInfluenceDto>();
    public OperationalTopologyContinuityDto TopologyContinuity { get; init; } = new();
    public string OperatorSummary { get; init; } = string.Empty;
    public string TopologyNote { get; init; } =
        "Advisory deterministic operational dependency interpretation from bounded continuity. Not distributed tracing, service meshes, or runtime topology discovery.";
}
