using MediatR;
using Tannous.Pos.Application.DTOs.Reservations;

namespace Tannous.Pos.Application.Reservations.Commands.CreateReservation;

public class CreateReservationCommand : IRequest<ReservationDto>
{
    public string   CustomerName        { get; set; } = string.Empty;
    public string?  CustomerPhone       { get; set; }
    public int      PartySize           { get; set; } = 2;
    public DateTime ReservationDateTime { get; set; }
    public string?  Notes               { get; set; }
    public Guid?    TableId             { get; set; }
    public Guid?    BranchId            { get; set; }
}
