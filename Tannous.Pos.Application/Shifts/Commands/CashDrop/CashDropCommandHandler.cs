using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Application.DTOs.Shifts;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Shifts.Commands.CashDrop;

public class CashDropCommandHandler : IRequestHandler<CashDropCommand, CashDrawerEventDto>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly DbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;

    public CashDropCommandHandler(
        IShiftRepository shiftRepository,
        DbContext dbContext,
        IUnitOfWork unitOfWork)
    {
        _shiftRepository = shiftRepository;
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<CashDrawerEventDto> Handle(CashDropCommand request, CancellationToken cancellationToken)
    {
        var shift = await _shiftRepository.GetByIdAsync(request.ShiftId);
        if (shift == null)
            throw new InvalidOperationException($"Shift {request.ShiftId} not found");

        if (shift.Status != ShiftStatus.Open)
            throw new InvalidOperationException($"Shift {request.ShiftId} is not in Open status");

        // Create cash drawer event
        var cashDrawerEvent = new CashDrawerEvent
        {
            ShiftId = request.ShiftId,
            EventType = "Drop",
            Amount = request.Amount,
            Notes = request.Note,
            EventDate = DateTime.UtcNow,
            Timestamp = DateTime.UtcNow
        };

        // Insert event without mutating Shift.RowVersion (append-only cash events).
        await _dbContext.Set<CashDrawerEvent>().AddAsync(cashDrawerEvent, cancellationToken);

        await _unitOfWork.SaveChangesAsync();

        return new CashDrawerEventDto
        {
            Id = cashDrawerEvent.Id,
            ShiftId = cashDrawerEvent.ShiftId,
            EventType = cashDrawerEvent.EventType,
            Amount = cashDrawerEvent.Amount,
            Timestamp = cashDrawerEvent.Timestamp,
            Note = cashDrawerEvent.Notes
        };
    }
}
