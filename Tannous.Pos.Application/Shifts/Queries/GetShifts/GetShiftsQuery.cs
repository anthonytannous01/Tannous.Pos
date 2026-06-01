using MediatR;
using Tannous.Pos.Application.DTOs.Shifts;

namespace Tannous.Pos.Application.Shifts.Queries.GetShifts;

public class GetShiftsQuery : IRequest<IEnumerable<ShiftDto>>
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate   { get; set; }
}
