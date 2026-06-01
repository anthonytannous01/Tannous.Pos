namespace Tannous.Pos.Application.OperationalExperienceGraph;

/// <summary>Bounded operational traversal paths for investigation continuity.</summary>
public sealed class OperationalExperienceTraversalPathsDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public int PathCount { get; init; }
    public IReadOnlyList<OperationalTraversalPathDto> TraversalPaths { get; init; } = Array.Empty<OperationalTraversalPathDto>();
    public string ExperienceNote { get; init; } =
        "Advisory deterministic traversal guidance for operational intelligence surfaces.";
}
