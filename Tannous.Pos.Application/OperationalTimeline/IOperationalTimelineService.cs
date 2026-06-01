namespace Tannous.Pos.Application.OperationalTimeline;

public interface IOperationalTimelineService
{
    Task<OperationalTimelineDto> GetTimelineAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OperationalTimelineCorrelationDto>> GetCorrelationsAsync(CancellationToken cancellationToken = default);
}
