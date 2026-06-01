namespace Tannous.Pos.Application.OperationalIntegrity;

/// <summary>Deterministic cross-layer operational interpretation contradiction.</summary>
public sealed class OperationalContradictionDto
{
    public string ContradictionId { get; init; } = string.Empty;
    public string SourceLayer { get; init; } = string.Empty;
    public string TargetLayer { get; init; } = string.Empty;
    public OperationalContradictionType ContradictionType { get; init; }
    public string Description { get; init; } = string.Empty;
    public OperationalIntegritySeverity Severity { get; init; }
    public string OperationalRisk { get; init; } = string.Empty;
    public string RecommendedOperatorReview { get; init; } = string.Empty;
}
