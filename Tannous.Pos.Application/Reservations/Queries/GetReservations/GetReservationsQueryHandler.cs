using MediatR;
using Tannous.Pos.Application.DTOs.Reservations;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Reservations.Queries.GetReservations;

public class GetReservationsQueryHandler
    : IRequestHandler<GetReservationsQuery, IEnumerable<ReservationDto>>
{
    private readonly IReservationRepository _repo;

    public GetReservationsQueryHandler(IReservationRepository repo) => _repo = repo;

    public async Task<IEnumerable<ReservationDto>> Handle(
        GetReservationsQuery request, CancellationToken ct)
    {
        var items = await _repo.GetAsync(
            request.BranchId, request.From, request.To, request.Status, ct);
        return items.Select(Map);
    }

    internal static ReservationDto Map(Reservation r) => new()
    {
        Id                  = r.Id,
        CustomerName        = r.CustomerName,
        CustomerPhone       = r.CustomerPhone,
        PartySize           = r.PartySize,
        ReservationDateTime = r.ReservationDateTime,
        Notes               = r.Notes,
        Status              = (int)r.Status,
        StatusName          = r.Status.ToString(),
        TableId             = r.TableId,
        TableNumber         = r.Table?.TableNumber,
        FloorPlanName       = r.Table?.FloorPlan?.Name,
        BranchId            = r.BranchId,
        CreatedAt           = r.CreatedAt
    };
}
