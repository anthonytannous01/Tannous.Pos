namespace Tannous.Pos.Application.OperationalPlaybooks;

/// <summary>Deterministic operator-facing operational response playbook.</summary>
public sealed class OperationalPlaybookDto
{
    public string PlaybookId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public OperationalPlaybookScenarioType ScenarioType { get; init; }
    public string DominantArea { get; init; } = string.Empty;
    public OperationalGuidanceSeverity Severity { get; init; }
    public string StabilizationObjective { get; init; } = string.Empty;
    public IReadOnlyList<string> RecommendedSequence { get; init; } = Array.Empty<string>();
    public string EstimatedOperationalImpact { get; init; } = string.Empty;
    public string RecoveryAlignment { get; init; } = string.Empty;
    public OperationalResponseConfidence OperationalConfidence { get; init; }
    public string OperatorSummary { get; init; } = string.Empty;
}
