using MediatR;
using Tannous.Pos.Application.DTOs.Shifts;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Shifts.Commands.KickCashDrawer;

public class KickCashDrawerCommandHandler : IRequestHandler<KickCashDrawerCommand, KickCashDrawerResult>
{
    private readonly IShiftRepository _shiftRepository;

    public KickCashDrawerCommandHandler(IShiftRepository shiftRepository)
    {
        _shiftRepository = shiftRepository;
    }

    public async Task<KickCashDrawerResult> Handle(
        KickCashDrawerCommand command, CancellationToken cancellationToken)
    {
        // GetOpenShiftByUserAsync does NOT use AsNoTracking — entity is tracked.
        // Mutation (adding to CashDrawerEvents) requires EF tracking to persist via CommitAsync.
        var shift = await _shiftRepository.GetOpenShiftByUserAsync(command.UserId);
        if (shift == null)
            return new KickCashDrawerResult { ShiftFound = false };

        var cashDrawerEvent = new CashDrawerEvent
        {
            ShiftId   = shift.Id,
            EventType = command.EventType,
            Amount    = command.Amount,
            Notes     = command.Note,
            Timestamp = DateTime.UtcNow
            // EventDate intentionally not set — matches original controller behavior
        };

        shift.CashDrawerEvents.Add(cashDrawerEvent);
        await _shiftRepository.CommitAsync(cancellationToken);

        return new KickCashDrawerResult
        {
            ShiftFound = true,
            Event = new CashDrawerEventDto
            {
                Id        = cashDrawerEvent.Id,
                ShiftId   = cashDrawerEvent.ShiftId,
                EventType = cashDrawerEvent.EventType,
                Amount    = cashDrawerEvent.Amount,
                Timestamp = cashDrawerEvent.Timestamp,
                Note      = cashDrawerEvent.Notes
            }
        };
    }
}
