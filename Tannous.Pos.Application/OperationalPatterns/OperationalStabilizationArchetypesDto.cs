namespace Tannous.Pos.Application.OperationalPatterns;

/// <summary>Bounded stabilization archetype recognition results.</summary>
public sealed class OperationalStabilizationArchetypesDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public int ArchetypeCount { get; init; }
    public IReadOnlyList<OperationalStabilizationArchetypeDto> Archetypes { get; init; } = Array.Empty<OperationalStabilizationArchetypeDto>();
    public string PatternNote { get; init; } =
        "Advisory deterministic archetype recognition from bounded process-local continuity. Not ML or predictive clustering.";
}
