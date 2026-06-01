namespace Tannous.Pos.Application.OperationalTriage;

/// <summary>Unified operator investigation queue item (advisory prioritization only).</summary>
public sealed class OperationalTriageItemDto
{
    public int Priority { get; init; }
    public OperationalTriagePriority PriorityBand { get; init; }
    public OperationalTriageCategory Category { get; init; }
    public OperationalTriageState State { get; init; }
    public string Summary { get; init; } = string.Empty;
    public string RecommendedRoute { get; init; } = string.Empty;
    public string InvestigationReason { get; init; } = string.Empty;
    public IReadOnlyList<string> CorrelatedSignals { get; init; } = Array.Empty<string>();
    public string SuggestedOperatorAction { get; init; } = string.Empty;
}
