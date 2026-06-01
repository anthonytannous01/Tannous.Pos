namespace Tannous.Pos.Application.OperationalRecovery;

/// <summary>Bounded recovery outlook section for a single operational domain.</summary>
public sealed class OperationalRecoveryOutlookSectionDto
{
    public string SectionId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public OperationalRecoveryState State { get; init; }
    public OperationalRecoveryDirection Direction { get; init; }
    public OperationalRecoveryConfidence Confidence { get; init; }
    public OperationalRecoverySeverity Severity { get; init; }
    public string Summary { get; init; } = string.Empty;
    public string RecommendedRoute { get; init; } = string.Empty;
}
