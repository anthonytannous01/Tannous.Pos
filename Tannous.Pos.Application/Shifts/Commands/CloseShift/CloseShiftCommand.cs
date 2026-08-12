using MediatR;
using Tannous.Pos.Application.DTOs.Shifts;

namespace Tannous.Pos.Application.Shifts.Commands.CloseShift;

public class CloseShiftCommand : IRequest<ShiftDto>
{
    public Guid ShiftId { get; set; }
    public decimal ClosingCount { get; set; }
    /// <summary>Counted LBP notes at close. 0 when no LBP was in the drawer.</summary>
    public decimal ClosingCountLbp { get; set; }
    public string? Note { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}
