using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Application.DTOs.Reports;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Reports.Queries.GetSalesSummary;

public class GetSalesSummaryQueryHandler : IRequestHandler<GetSalesSummaryQuery, SalesSummaryDto>
{
    private readonly DbContext _dbContext;

    public GetSalesSummaryQueryHandler(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SalesSummaryDto> Handle(GetSalesSummaryQuery request, CancellationToken cancellationToken)
    {
        var from = (request.From ?? DateTime.UtcNow.Date);
        var to   = (request.To   ?? DateTime.UtcNow);

        // Load all orders in range (Paid + Void) with lines and payments
        var query = _dbContext.Set<Order>()
            .Include(o => o.OrderLines)
                .ThenInclude(ol => ol.MenuItem)
            .Include(o => o.Payments)
            .Where(o => o.CreatedAt >= from && o.CreatedAt < to
                && (o.Status == OrderStatus.Paid || o.Status == OrderStatus.Void));

        if (request.BranchId.HasValue)
            query = query.Where(o => o.BranchId == request.BranchId.Value);

        var orders = await query.ToListAsync(cancellationToken);

        var paidOrders  = orders.Where(o => o.Status == OrderStatus.Paid).ToList();
        var voidedCount = orders.Count(o => o.Status == OrderStatus.Void);
        var totalSeen   = orders.Count;

        // Core metrics
        var netSales      = paidOrders.Sum(o => o.TotalAmount);
        var taxCollected  = paidOrders.Sum(o => o.TaxAmount);
        var stampCollected= paidOrders.Sum(o => o.StampDutyAmount);
        var grossSales    = paidOrders.Sum(o => o.SubTotal);
        var ordersCount   = paidOrders.Count;
        var voidRate      = totalSeen > 0
            ? Math.Round((decimal)voidedCount / totalSeen * 100, 1)
            : 0m;
        var avgTicket     = ordersCount > 0 ? Math.Round(netSales / ordersCount, 2) : 0m;
        var totalItems    = paidOrders.Sum(o => o.OrderLines.Sum(ol => ol.Quantity));
        var avgItems      = ordersCount > 0 ? Math.Round(totalItems / ordersCount, 1) : 0m;

        // Order type split
        var dineIn   = paidOrders.Count(o => o.OrderType == OrderType.DineIn);
        var takeaway = paidOrders.Count(o => o.OrderType == OrderType.Takeaway);
        var delivery = paidOrders.Count(o => o.OrderType == OrderType.Delivery);

        // Payment method breakdown
        var paymentMethods = paidOrders
            .SelectMany(o => o.Payments)
            .GroupBy(p => new { p.PaymentMethod, p.TenderedCurrency })
            .Select(g => new PaymentMethodSummaryDto
            {
                Method   = g.Key.PaymentMethod,
                Currency = g.Key.TenderedCurrency,
                Amount   = g.Sum(p => p.Amount),
                Count    = g.Count()
            })
            .OrderByDescending(p => p.Amount)
            .ToList();

        // Top items by sales value (top 8)
        var topItems = paidOrders
            .SelectMany(o => o.OrderLines)
            .GroupBy(ol => new { ol.MenuItemId, ol.MenuItem.Name })
            .Select(g => new TopItemDto
            {
                ItemId = g.Key.MenuItemId,
                Name   = g.Key.Name,
                Qty    = (int)g.Sum(ol => ol.Quantity),
                Sales  = g.Sum(ol => ol.TotalPrice)
            })
            .OrderByDescending(ti => ti.Sales)
            .Take(8)
            .ToList();

        // Hourly breakdown
        var hourlySales = paidOrders
            .GroupBy(o => o.ClosedAt.HasValue ? o.ClosedAt.Value.Hour : o.CreatedAt.Hour)
            .Select(g => new HourlySalesDto
            {
                Hour   = g.Key,
                Sales  = g.Sum(o => o.TotalAmount),
                Orders = g.Count()
            })
            .OrderBy(h => h.Hour)
            .ToList();

        return new SalesSummaryDto
        {
            From                 = from,
            To                   = to,
            NetSales             = netSales,
            TaxCollected         = taxCollected,
            StampDutyCollected   = stampCollected,
            GrossSales           = grossSales,
            OrdersCount          = ordersCount,
            VoidedOrdersCount    = voidedCount,
            VoidRate             = voidRate,
            AvgTicket            = avgTicket,
            AvgItemsPerOrder     = avgItems,
            DineInCount          = dineIn,
            TakeawayCount        = takeaway,
            DeliveryCount        = delivery,
            PaymentMethods       = paymentMethods,
            TopItems             = topItems,
            HourlySales          = hourlySales
        };
    }
}
