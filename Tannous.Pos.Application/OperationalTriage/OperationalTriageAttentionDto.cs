namespace Tannous.Pos.Application.OperationalTriage;

/// <summary>Operator triage attention item (advisory guidance only).</summary>
public sealed class OperationalTriageAttentionDto
{
    public int Priority { get; init; }
    public OperationalTriagePriority PriorityBand { get; init; }
    public OperationalTriageCategory Category { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;
    public string RecommendedRoute { get; init; } = string.Empty;
}
