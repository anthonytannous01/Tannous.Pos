namespace Tannous.Pos.Application.OperationalSituationRoom;

/// <summary>Template-driven operational narrative segment.</summary>
public sealed class OperationalNarrativeDto
{
    public OperationalNarrativeType NarrativeType { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public OperationalExecutiveSeverity Severity { get; init; }
    public OperationalSituationDirection Direction { get; init; }
    public string RelatedArea { get; init; } = string.Empty;
    public string OperatorInterpretation { get; init; } = string.Empty;
}
