namespace Tannous.Pos.Application.OperationalIncidents;

/// <summary>Operator incident cases and investigation sessions (advisory; process-local).</summary>
public interface IOperationalIncidentService
{
    Task<OperationalIncidentCasesDto> GetIncidentCasesAsync(CancellationToken cancellationToken = default);

    Task<OperationalIncidentCasesSummaryDto> GetIncidentSummaryAsync(CancellationToken cancellationToken = default);

    Task<OperationalIncidentCaseDetailDto?> GetIncidentDetailsAsync(
        string incidentId,
        CancellationToken cancellationToken = default);
}
