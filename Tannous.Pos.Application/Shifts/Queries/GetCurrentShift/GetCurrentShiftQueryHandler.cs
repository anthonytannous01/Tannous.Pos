using MediatR;
using Tannous.Pos.Application.DTOs.Shifts;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Shifts.Queries.GetCurrentShift;

public class GetCurrentShiftQueryHandler : IRequestHandler<GetCurrentShiftQuery, ShiftDto?>
{
    private readonly IShiftRepository _shiftRepository;

    public GetCurrentShiftQueryHandler(IShiftRepository shiftRepository)
    {
        _shiftRepository = shiftRepository;
    }

    public async Task<ShiftDto?> Handle(GetCurrentShiftQuery query, CancellationToken cancellationToken)
    {
        var openShift = await _shiftRepository.GetOpenShiftByUserAsync(query.UserId);
        if (openShift == null) return null;

        // The stored ExpectedCash is only finalized at CloseShift; while the shift is open
        // it still holds the opening balance. Reload with orders/payments/drawer events and
        // compute live so the active-shift screen reflects cash sales as they happen.
        var shift = await _shiftRepository.GetByIdWithDetailsAsync(openShift.Id) ?? openShift;
        var liveExpectedCash = ShiftCashCalculator.ComputeExpectedCash(shift);

        return MapToDto(shift, liveExpectedCash);
    }

    private static ShiftDto MapToDto(Shift s, decimal expectedCash) => new()
    {
        Id             = s.Id,
        ShiftNumber    = s.ShiftNumber,
        StartTime      = s.StartTime,
        EndTime        = s.EndTime,
        OpeningBalance = s.OpeningBalance,
        ClosingBalance = s.ClosingBalance,
        ExpectedCash   = expectedCash,
        ActualCash     = s.ActualCash,
        CashDifference = s.CashDifference,
        Status         = s.Status.ToString(),
        Notes          = s.Notes,
        UserId         = s.UserId,
        CreatedAt      = s.CreatedAt
    };
}
