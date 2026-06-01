namespace Tannous.Pos.Application.OperationalResilience;

/// <summary>Deterministic operational resilience and survivability intelligence (advisory; GET-only).</summary>
public interface IOperationalResilienceCognitionService
{
    Task<OperationalResilienceReportDto> GetResilienceReportAsync(CancellationToken cancellationToken = default);

    Task<OperationalResiliencePostureSummaryDto> GetResilienceSummaryAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OperationalFragilityDto>> GetOperationalFragilityAsync(CancellationToken cancellationToken = default);
}
