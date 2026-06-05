using MediatR;
using Tannous.Pos.Application.DTOs.Reservations;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Reservations.Queries.GetAvailableTables;

public class GetAvailableTablesQueryHandler
    : IRequestHandler<GetAvailableTablesQuery, IEnumerable<AvailableTableDto>>
{
    private readonly IReservationRepository _reservationRepo;
    private readonly ITableRepository       _tableRepo;

    public GetAvailableTablesQueryHandler(
        IReservationRepository reservationRepo,
        ITableRepository       tableRepo)
    {
        _reservationRepo = reservationRepo;
        _tableRepo       = tableRepo;
    }

    public async Task<IEnumerable<AvailableTableDto>> Handle(
        GetAvailableTablesQuery request, CancellationToken ct)
    {
        // Get all active tables with sufficient capacity
        var allTables = await _tableRepo.GetActiveAsync(request.PartySize, ct);

        // Get tables already booked within ±2 hours of the requested slot
        var conflicting = new HashSet<Guid>(
            await _reservationRepo.GetConflictingTableIdsAsync(request.SlotDateTime, ct));

        return allTables
            .Where(t => !conflicting.Contains(t.Id))
            .Select(t => new AvailableTableDto
            {
                Id          = t.Id,
                TableNumber = t.TableNumber,
                Label       = t.Label,
                Capacity    = t.Capacity,
                FloorPlan   = t.FloorPlan?.Name ?? string.Empty
            });
    }
}
