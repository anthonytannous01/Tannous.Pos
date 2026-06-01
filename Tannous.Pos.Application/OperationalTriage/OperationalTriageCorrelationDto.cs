namespace Tannous.Pos.Application.OperationalTriage;

/// <summary>Deterministic triage correlation (heuristic only; not causal inference).</summary>
public sealed class OperationalTriageCorrelationDto
{
    public string CorrelationLabel { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public OperationalTriagePriority Priority { get; init; }
    public IReadOnlyList<string> RelatedCategories { get; init; } = Array.Empty<string>();
    public string RecommendedRoute { get; init; } = string.Empty;
}
