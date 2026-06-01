namespace Tannous.Pos.Application.OperationalSituationRoom;

/// <summary>Operator situation room and executive briefing synthesis (advisory, process-local).</summary>
public interface IOperationalSituationRoomService
{
    Task<OperationalSituationRoomDto> GetSituationRoomAsync(CancellationToken cancellationToken = default);

    Task<OperationalExecutiveBriefingDto> GetExecutiveBriefingAsync(CancellationToken cancellationToken = default);

    Task<OperationalSituationSummaryDto> GetSituationSummaryAsync(CancellationToken cancellationToken = default);
}
