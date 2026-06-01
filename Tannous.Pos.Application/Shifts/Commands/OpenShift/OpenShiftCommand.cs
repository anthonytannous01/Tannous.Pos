using MediatR;
using Tannous.Pos.Application.DTOs.Shifts;

namespace Tannous.Pos.Application.Shifts.Commands.OpenShift;

public class OpenShiftCommand : IRequest<ShiftDto>
{
    public decimal OpeningBalance { get; set; }
    public Guid UserId { get; set; }
    public string? Notes { get; set; }
}
