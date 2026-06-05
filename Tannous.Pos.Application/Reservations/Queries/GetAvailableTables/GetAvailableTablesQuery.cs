using MediatR;
using Tannous.Pos.Application.DTOs.Reservations;

namespace Tannous.Pos.Application.Reservations.Queries.GetAvailableTables;

public class GetAvailableTablesQuery : IRequest<IEnumerable<AvailableTableDto>>
{
    public DateTime SlotDateTime { get; set; }
    public int      PartySize    { get; set; } = 1;
    public Guid?    BranchId     { get; set; }
}
