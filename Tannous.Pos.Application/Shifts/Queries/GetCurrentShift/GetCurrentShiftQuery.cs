using MediatR;
using Tannous.Pos.Application.DTOs.Shifts;

namespace Tannous.Pos.Application.Shifts.Queries.GetCurrentShift;

public class GetCurrentShiftQuery : IRequest<ShiftDto?>
{
    public Guid UserId { get; set; }
}
