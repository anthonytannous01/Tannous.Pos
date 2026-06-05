using MediatR;
using Tannous.Pos.Application.DTOs.Reservations;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Reservations.Commands.UpdateReservationStatus;

public class UpdateReservationStatusCommand : IRequest<ReservationDto>
{
    public Guid              ReservationId { get; set; }
    public ReservationStatus NewStatus     { get; set; }
    /// <summary>Optional table assignment when seating a walk-in or updating assignment.</summary>
    public Guid?             TableId       { get; set; }
}
