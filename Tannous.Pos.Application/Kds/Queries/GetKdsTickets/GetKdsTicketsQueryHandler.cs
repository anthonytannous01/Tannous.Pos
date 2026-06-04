using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Application.DTOs.Kds;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Kds.Queries.GetKdsTickets;

public class GetKdsTicketsQueryHandler : IRequestHandler<GetKdsTicketsQuery, List<KdsTicketDto>>
{
    private readonly DbContext _dbContext;

    public GetKdsTicketsQueryHandler(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<KdsTicketDto>> Handle(GetKdsTicketsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<OrderLine>()
            .Include(ol => ol.Order)
            .Include(ol => ol.MenuItem)
            .Include(ol => ol.OrderLineAddOns)
                .ThenInclude(ola => ola.AddOn)
            .Where(ol => ol.Order.Status == OrderStatus.Open || ol.Order.Status == OrderStatus.Pending);

        // Apply status filter — default to active (Pending + InProgress)
        if (request.StatusFilter.HasValue)
        {
            query = query.Where(ol => ol.KdsStatus == request.StatusFilter.Value);
        }
        else
        {
            query = query.Where(ol =>
                ol.KdsStatus == KdsStatus.Pending ||
                ol.KdsStatus == KdsStatus.InProgress);
        }

        var lines = await query
            .OrderBy(ol => ol.Order.CreatedAt)
            .ThenBy(ol => ol.CreatedAt)
            .ToListAsync(cancellationToken);

        return lines.Select(ol => new KdsTicketDto
        {
            OrderLineId      = ol.Id,
            OrderId          = ol.OrderId,
            OrderNumber      = ol.Order.OrderNumber,
            OrderType        = ol.Order.OrderType,
            MenuItemName     = ol.MenuItem.Name,
            Quantity         = ol.Quantity,
            Notes            = ol.Notes,
            AddOns           = ol.OrderLineAddOns.Select(a => a.AddOn.Name).ToList(),
            KdsStatus        = ol.KdsStatus,
            OrderCreatedAt   = ol.Order.CreatedAt,
            KdsAcknowledgedAt = ol.KdsAcknowledgedAt,
            KdsDoneAt        = ol.KdsDoneAt
        }).ToList();
    }
}
