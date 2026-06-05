using MediatR;
using Tannous.Pos.Application.DTOs.Shifts;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Shifts.Queries.GetShifts;

public class GetShiftsQueryHandler : IRequestHandler<GetShiftsQuery, IEnumerable<ShiftDto>>
{
    private readonly IShiftRepository _shiftRepository;

    public GetShiftsQueryHandler(IShiftRepository shiftRepository)
    {
        _shiftRepository = shiftRepository;
    }

    public async Task<IEnumerable<ShiftDto>> Handle(GetShiftsQuery query, CancellationToken cancellationToken)
    {
        // Preserve original behavior: load all shifts, filter in memory.
        // Do NOT replace with GetByDateRangeAsync — that method requires both date params
        // and its inclusive bounds differ from the original open-ended filter semantics.
        var shifts = await _shiftRepository.GetAllAsync();

        if (query.StartDate.HasValue)
            shifts = shifts.Where(s => s.StartTime >= query.StartDate.Value);

        if (query.EndDate.HasValue)
            shifts = shifts.Where(s => s.StartTime <= query.EndDate.Value);

        if (query.BranchId.HasValue)
            shifts = shifts.Where(s => s.BranchId == query.BranchId.Value);

        return shifts.Select(MapToDto).ToList();
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
