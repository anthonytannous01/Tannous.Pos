using MediatR;
using Tannous.Pos.Application.DTOs.Shifts;

namespace Tannous.Pos.Application.Shifts.Commands.CashDrop;

public class CashDropCommand : IRequest<CashDrawerEventDto>
{
    public Guid ShiftId { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}
