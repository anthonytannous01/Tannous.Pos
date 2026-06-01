namespace Tannous.Pos.Application.OperationalTriage;

public interface IOperationalTriageService
{
    Task<OperationalTriageQueueDto> GetTriageQueueAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OperationalTriageRecommendationDto>> GetRecommendationsAsync(
        CancellationToken cancellationToken = default);
}
