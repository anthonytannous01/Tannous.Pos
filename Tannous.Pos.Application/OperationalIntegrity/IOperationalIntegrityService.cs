namespace Tannous.Pos.Application.OperationalIntegrity;

/// <summary>Operator operational interpretation integrity and consistency verification (advisory, process-local).</summary>
public interface IOperationalIntegrityService
{
    Task<OperationalIntegrityReportDto> GetIntegrityReportAsync(CancellationToken cancellationToken = default);

    Task<OperationalIntegritySummaryDto> GetIntegritySummaryAsync(CancellationToken cancellationToken = default);

    Task<OperationalIntegrityContradictionsDto> GetContradictionsAsync(CancellationToken cancellationToken = default);
}
