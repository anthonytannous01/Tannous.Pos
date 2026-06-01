namespace Tannous.Pos.Application.Audit;

/// <summary>Additive forensic export incident correlation enrichment.</summary>
public sealed class ForensicIncidentCorrelationDto
{
    public string CorrelatedIncidentRisk { get; init; } = OperationalIncidentSeverity.Low;
    public IReadOnlyList<string> CorrelatedSubsystems { get; init; } = Array.Empty<string>();
    public string IncidentCorrelationSummary { get; init; } = string.Empty;
}
