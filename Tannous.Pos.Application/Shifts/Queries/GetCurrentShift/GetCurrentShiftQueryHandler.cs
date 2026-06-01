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
        var shift = await _shiftRepository.GetOpenShiftByUserAsync(query.UserId);
        if (shift == null) return null;
        return MapToDto(shift);
    }

    private static ShiftDto MapToDto(Shift s) => new()
    {
        Id             = s.Id,
        ShiftNumber    = s.ShiftNumber,
        StartTime      = s.StartTime,
        EndTime        = s.EndTime,
        OpeningBalance = s.OpeningBalance,
        ClosingBalance = s.ClosingBalance,
        ExpectedCash   = s.ExpectedCash,
        ActualCash     = s.ActualCash,
        CashDifference = s.CashDifference,
        Status         = s.Status.ToString(),
        Notes          = s.Notes,
        UserId         = s.UserId,
        CreatedAt      = s.CreatedAt
    };
}
