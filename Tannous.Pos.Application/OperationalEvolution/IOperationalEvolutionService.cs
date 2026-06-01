namespace Tannous.Pos.Application.OperationalEvolution;

/// <summary>Operator operational evolution timeline and transition intelligence (advisory, process-local).</summary>
public interface IOperationalEvolutionService
{
    Task<OperationalEvolutionTimelineDto> GetEvolutionTimelineAsync(CancellationToken cancellationToken = default);

    Task<OperationalEvolutionSummaryDto> GetEvolutionSummaryAsync(CancellationToken cancellationToken = default);

    Task<OperationalMomentumAnalysisDto> GetMomentumAnalysisAsync(CancellationToken cancellationToken = default);
}
