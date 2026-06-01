namespace Tannous.Pos.Application.Audit;

public interface IOperationalRetentionSummaryService
{
    Task<OperationalRetentionSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
}
