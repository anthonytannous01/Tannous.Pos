namespace Tannous.Pos.Application.OperationalExperienceGraph;

/// <summary>Deterministic recommended operational traversal sequence.</summary>
public sealed class OperationalTraversalPathDto
{
    public string PathId { get; init; } = string.Empty;
    public string StartingSurface { get; init; } = string.Empty;
    public IReadOnlyList<string> RecommendedSequence { get; init; } = Array.Empty<string>();
    public string DominantOperationalFocus { get; init; } = string.Empty;
    public string ExpectedOperatorOutcome { get; init; } = string.Empty;
    public OperationalTraversalPriority TraversalPriority { get; init; }
    public string OperatorSummary { get; init; } = string.Empty;
}
