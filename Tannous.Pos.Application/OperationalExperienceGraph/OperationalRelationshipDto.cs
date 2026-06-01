namespace Tannous.Pos.Application.OperationalExperienceGraph;

/// <summary>Deterministic relationship between two operational intelligence surfaces.</summary>
public sealed class OperationalRelationshipDto
{
    public string SourceSurface { get; init; } = string.Empty;
    public string TargetSurface { get; init; } = string.Empty;
    public OperationalRelationshipType RelationshipType { get; init; }
    public OperationalContextType OperationalContext { get; init; }
    public OperationalNavigationStrength RelevanceStrength { get; init; }
    public string TraversalReason { get; init; } = string.Empty;
    public string OperatorInterpretation { get; init; } = string.Empty;
}
