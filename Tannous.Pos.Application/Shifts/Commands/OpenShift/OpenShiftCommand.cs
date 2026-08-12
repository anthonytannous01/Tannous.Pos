using MediatR;
using Tannous.Pos.Application.DTOs.Shifts;

namespace Tannous.Pos.Application.Shifts.Commands.OpenShift;

public class OpenShiftCommand : IRequest<ShiftDto>
{
    public decimal OpeningBalance { get; set; }
    /// <summary>Opening LBP float in the same physical drawer. 0 when the till starts without LBP.</summary>
    public decimal OpeningBalanceLbp { get; set; }
    public Guid UserId { get; set; }
    public string? Notes { get; set; }
    /// <summary>Branch this shift is opened at. Null falls back to the default branch.</summary>
    public Guid? BranchId { get; set; }
}
