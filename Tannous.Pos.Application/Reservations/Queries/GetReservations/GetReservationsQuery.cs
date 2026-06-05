using MediatR;
using Tannous.Pos.Application.DTOs.Reservations;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Reservations.Queries.GetReservations;

public class GetReservationsQuery : IRequest<IEnumerable<ReservationDto>>
{
    public Guid?              BranchId { get; set; }
    public DateTime?          From     { get; set; }
    public DateTime?          To       { get; set; }
    public ReservationStatus? Status   { get; set; }
}
