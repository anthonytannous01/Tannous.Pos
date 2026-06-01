namespace Tannous.Pos.Application.OperationalConvergence;

/// <summary>Deterministic operational signal convergence intelligence (advisory; GET-only).</summary>
public interface IOperationalConvergenceService
{
    Task<OperationalConvergenceReportDto> GetConvergenceReportAsync(CancellationToken cancellationToken = default);

    Task<OperationalConvergenceSummaryDto> GetConvergenceSummaryAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OperationalDivergenceDto>> GetOperationalDivergenceAsync(CancellationToken cancellationToken = default);
}
