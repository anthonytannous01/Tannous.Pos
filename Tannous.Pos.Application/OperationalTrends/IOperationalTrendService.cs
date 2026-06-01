namespace Tannous.Pos.Application.OperationalTrends;

public interface IOperationalTrendService
{
    Task<OperationalTrendSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OperationalTrendDeltaDto>> GetDeltasAsync(CancellationToken cancellationToken = default);
}
