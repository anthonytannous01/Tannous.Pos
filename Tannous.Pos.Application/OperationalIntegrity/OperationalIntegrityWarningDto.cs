namespace Tannous.Pos.Application.OperationalIntegrity;

/// <summary>Advisory integrity warning for operator review.</summary>
public sealed class OperationalIntegrityWarningDto
{
    public string WarningType { get; init; } = string.Empty;
    public string RelatedArea { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public OperationalIntegritySeverity Severity { get; init; }
    public string OperationalImpact { get; init; } = string.Empty;
    public string SuggestedOperatorFocus { get; init; } = string.Empty;
}
