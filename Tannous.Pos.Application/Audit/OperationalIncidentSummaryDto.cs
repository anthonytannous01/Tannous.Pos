namespace Tannous.Pos.Application.Audit;

public sealed class OperationalIncidentSummaryDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public int TotalIncidentGroups { get; init; }
    public int HighRiskIncidentCount { get; init; }
    public int CriticalIncidentCount { get; init; }
    public int ReplayIncidentCount { get; init; }
    public int ReconciliationIncidentCount { get; init; }
    public int CascadingDegradationCount { get; init; }
    public string OverallCorrelatedRisk { get; init; } = OperationalIncidentSeverity.Low;
    public IReadOnlyDictionary<string, int> IncidentsBySeverity { get; init; } =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, string> CorrelationGuidance { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
