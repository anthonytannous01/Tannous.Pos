namespace Tannous.Pos.Application.OperationalExperienceGraph;

/// <summary>Deterministic operational experience relationship graph.</summary>
public sealed class OperationalExperienceGraphDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public OperationalContextType DominantOperationalContext { get; init; }
    public OperationalExperienceState ExperienceState { get; init; }
    public int ActiveRelationshipCount { get; init; }
    public string RecommendedEntryPoint { get; init; } = string.Empty;
    public string RecommendedTraversalPath { get; init; } = string.Empty;
    public IReadOnlyList<OperationalRelationshipDto> Relationships { get; init; } = Array.Empty<OperationalRelationshipDto>();
    public OperationalInvestigationContinuityDto InvestigationContinuity { get; init; } = new();
    public OperationalExperienceSummaryDto ExperienceSummary { get; init; } = new();
    public string OperatorSummary { get; init; } = string.Empty;
    public string ExperienceNote { get; init; } =
        "Advisory deterministic operational relationship mapping composed from existing intelligence layers. Not graph analytics, workflow routing, or frontend navigation.";
}
