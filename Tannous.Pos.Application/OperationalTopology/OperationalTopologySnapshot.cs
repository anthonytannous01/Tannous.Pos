namespace Tannous.Pos.Application.OperationalTopology;

/// <summary>Lightweight process-local topology snapshot for short-term continuity.</summary>
public sealed class OperationalTopologySnapshot
{
    public DateTime GeneratedAtUtc { get; init; }
    public string DominantOperationalTopology { get; init; } = string.Empty;
    public string HighestInfluenceArea { get; init; } = string.Empty;
    public OperationalTopologyState TopologyState { get; init; }
    public string StabilizationDependencyStrength { get; init; } = string.Empty;
    public string EscalationPropagationStrength { get; init; } = string.Empty;
    public int ActiveDependencyCount { get; init; }
}
