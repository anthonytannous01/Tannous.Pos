using MediatR;
using Tannous.Pos.Application.DTOs.Kds;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Kds.Queries.GetKdsTickets;

/// <summary>
/// Returns all active KDS tickets (Pending + InProgress) for the kitchen display.
/// Optionally filter by status to show only pending or only in-progress items.
/// </summary>
public class GetKdsTicketsQuery : IRequest<List<KdsTicketDto>>
{
    /// <summary>When null, returns Pending and InProgress lines. Pass a specific status to filter.</summary>
    public KdsStatus? StatusFilter { get; set; }

    /// <summary>When set, returns only tickets for menu items assigned to this station.</summary>
    public Guid? StationFilter { get; set; }
}
