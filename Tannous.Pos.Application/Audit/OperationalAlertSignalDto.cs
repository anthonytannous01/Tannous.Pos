namespace Tannous.Pos.Application.Audit;

/// <summary>Query-time operational alert signal (not persisted; not delivered externally).</summary>
public sealed class OperationalAlertSignalDto
{
    public string AlertType { get; init; } = string.Empty;
    public string Severity { get; init; } = OperationalAlertSeverity.Info;
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyList<string> Subsystems { get; init; } = Array.Empty<string>();
    public int RelatedConflictCount { get; init; }
    public DateTime GeneratedAtUtc { get; init; }
    public string EscalationRecommendation { get; init; } = string.Empty;
    public string PressureClassification { get; init; } = "Normal";
    public string IncidentRisk { get; init; } = OperationalIncidentSeverity.Low;
    public string SuggestedOperatorAction { get; init; } = string.Empty;
}
