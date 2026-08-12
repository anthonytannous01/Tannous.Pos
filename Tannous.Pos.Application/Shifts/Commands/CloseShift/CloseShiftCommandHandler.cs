using MediatR;
using Tannous.Pos.Application.DTOs.Shifts;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Domain.Interfaces;
using Tannous.Pos.Application.DTOs.Orders;

namespace Tannous.Pos.Application.Shifts.Commands.CloseShift;

public class CloseShiftCommandHandler : IRequestHandler<CloseShiftCommand, ShiftDto>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CloseShiftCommandHandler(
        IShiftRepository shiftRepository,
        IUnitOfWork unitOfWork)
    {
        _shiftRepository = shiftRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ShiftDto> Handle(CloseShiftCommand request, CancellationToken cancellationToken)
    {
        var shift = await _shiftRepository.GetByIdWithDetailsAsync(request.ShiftId);
        if (shift == null)
            throw new InvalidOperationException($"Shift {request.ShiftId} not found");

        if (shift.Status != ShiftStatus.Open)
            throw new InvalidOperationException($"Shift {request.ShiftId} is not in Open status");

        // Shared formula with GetCurrentShift so the live view and the closing figures agree.
        // Each currency reconciles independently — USD and LBP notes share the drawer but are never converted.
        var expectedCash    = ShiftCashCalculator.ComputeExpectedCash(shift);
        var actualCash      = request.ClosingCount;
        var variance        = actualCash - expectedCash;

        var expectedCashLbp = ShiftCashCalculator.ComputeExpectedCashLbp(shift);
        var actualCashLbp   = request.ClosingCountLbp;
        var varianceLbp     = actualCashLbp - expectedCashLbp;

        // Update shift
        shift.Status         = ShiftStatus.Closed;
        shift.EndTime        = DateTime.UtcNow;
        shift.ClosingBalance = actualCash;
        shift.ExpectedCash   = expectedCash;
        shift.ActualCash     = actualCash;
        shift.CashDifference = variance;
        shift.ExpectedCashLbp   = expectedCashLbp;
        shift.ActualCashLbp     = actualCashLbp;
        shift.CashDifferenceLbp = varianceLbp;
        shift.Notes          = request.Note;

        await _unitOfWork.SaveChangesAsync();

        return new ShiftDto
        {
            Id             = shift.Id,
            ShiftNumber    = shift.ShiftNumber,
            StartTime      = shift.StartTime,
            EndTime        = shift.EndTime,
            Status         = shift.Status.ToString(),
            OpeningBalance = shift.OpeningBalance,
            ClosingBalance = shift.ClosingBalance,
            ExpectedCash   = shift.ExpectedCash,
            ActualCash     = shift.ActualCash,
            CashDifference = shift.CashDifference,
            OpeningBalanceLbp = shift.OpeningBalanceLbp,
            ExpectedCashLbp   = shift.ExpectedCashLbp,
            ActualCashLbp     = shift.ActualCashLbp,
            CashDifferenceLbp = shift.CashDifferenceLbp,
            Notes          = shift.Notes,
            UserId         = shift.UserId,
            CreatedAt      = shift.CreatedAt
        };
    }
}
