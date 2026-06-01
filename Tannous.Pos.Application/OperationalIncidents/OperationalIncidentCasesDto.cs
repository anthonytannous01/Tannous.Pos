namespace Tannous.Pos.Application.OperationalIncidents;

/// <summary>Bounded list of operator incident cases.</summary>
public sealed class OperationalIncidentCasesDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public int CaseCount { get; init; }
    public int MaxCases { get; init; } = OperationalIncidentAggregation.MaxIncidentCases;
    public IReadOnlyList<OperationalIncidentCaseDto> Cases { get; init; } = Array.Empty<OperationalIncidentCaseDto>();
}
