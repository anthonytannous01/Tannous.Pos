using MediatR;
using Tannous.Pos.Application.DTOs.Shifts;

namespace Tannous.Pos.Application.Shifts.Commands.KickCashDrawer;

public class KickCashDrawerCommand : IRequest<KickCashDrawerResult>
{
    public Guid    UserId    { get; set; }
    public string  EventType { get; set; } = string.Empty;
    public decimal? Amount   { get; set; }
    public string? Note      { get; set; }
}

public class KickCashDrawerResult
{
    public bool              ShiftFound { get; init; }
    public CashDrawerEventDto? Event    { get; init; }
}
