namespace Tannous.Pos.Application.OperationalIncidents;

/// <summary>Bounded operator incident case (advisory; process-local; not persisted).</summary>
public sealed class OperationalIncidentCaseDto
{
    public string IncidentId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public OperationalIncidentSeverity Severity { get; init; }
    public OperationalIncidentState State { get; init; }
    public OperationalIncidentDirection Direction { get; init; }
    public OperationalIncidentConfidence Confidence { get; init; }
    public DateTime FirstObservedUtc { get; init; }
    public DateTime LastObservedUtc { get; init; }
    public OperationalInvestigationPriority InvestigationPriority { get; init; }
    public string RecoveryAlignment { get; init; } = string.Empty;
    public bool IsRecurring { get; init; }
    public bool IsEscalating { get; init; }
    public int ActiveSignalCount { get; init; }
    public int CorrelatedAreaCount { get; init; }
    public string RecommendedRoute { get; init; } = string.Empty;
    public string RecommendedWorkbench { get; init; } = string.Empty;
    public string EstimatedStabilization { get; init; } = string.Empty;
    public string OperatorSummary { get; init; } = string.Empty;
}
