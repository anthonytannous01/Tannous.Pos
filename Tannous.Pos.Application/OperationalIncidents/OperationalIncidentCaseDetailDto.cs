namespace Tannous.Pos.Application.OperationalIncidents;

/// <summary>Detailed operator incident investigation session (advisory; read-only).</summary>
public sealed class OperationalIncidentCaseDetailDto
{
    public OperationalIncidentCaseDto Case { get; init; } = new();
    public IReadOnlyList<OperationalIncidentSignalDto> Signals { get; init; } = Array.Empty<OperationalIncidentSignalDto>();
    public OperationalInvestigationContextDto InvestigationContext { get; init; } = new();
    public OperationalIncidentOutlookDto Outlook { get; init; } = new();
}
