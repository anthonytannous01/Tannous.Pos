namespace Tannous.Pos.Application.OperationalTopology;

/// <summary>Short-term topology continuity interpretation from bounded snapshots.</summary>
public sealed class OperationalTopologyContinuityDto
{
    public string DominantTopologyShift { get; init; } = string.Empty;
    public string DependencyStability { get; init; } = string.Empty;
    public string EscalationTopologyConsistency { get; init; } = string.Empty;
    public string StabilizationTopologyConsistency { get; init; } = string.Empty;
    public string RecoveryTopologyAlignment { get; init; } = string.Empty;
    public string OperatorInterpretation { get; init; } = string.Empty;
}
