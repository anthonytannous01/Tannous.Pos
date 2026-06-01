namespace Tannous.Pos.Application.OperationalExperienceGraph;

/// <summary>Lightweight process-local experience graph snapshot for short-term navigation continuity.</summary>
public sealed class OperationalExperienceSnapshot
{
    public DateTime GeneratedAtUtc { get; init; }
    public OperationalContextType DominantOperationalContext { get; init; }
    public OperationalExperienceState ExperienceState { get; init; }
    public int ActiveRelationshipCount { get; init; }
    public string RecommendedEntryPoint { get; init; } = string.Empty;
    public string DominantOperationalFlow { get; init; } = string.Empty;
}
