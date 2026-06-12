using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Application.DTOs.Reports;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Reports.Queries.GetSectionSales;

public class GetSectionSalesQueryHandler : IRequestHandler<GetSectionSalesQuery, SectionSalesReportDto>
{
    private const string NoSectionName = "No Section";

    private readonly DbContext _dbContext;

    public GetSectionSalesQueryHandler(DbContext dbContext) => _dbContext = dbContext;

    public async Task<SectionSalesReportDto> Handle(
        GetSectionSalesQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<Order>()
            .AsNoTracking()
            .Include(o => o.OrderLines)
                .ThenInclude(ol => ol.MenuItem)
            .Include(o => o.Table)
                .ThenInclude(t => t!.FloorPlan)
            .Where(o => o.Status == OrderStatus.Paid
                && o.CreatedAt >= request.From
                && o.CreatedAt < request.To);

        if (request.BranchId.HasValue)
            query = query.Where(o => o.BranchId == request.BranchId);

        var orders = await query.ToListAsync(cancellationToken);

        var grouped = orders.GroupBy(o =>
        {
            if (o.Table?.FloorPlan != null)
                return (Name: o.Table.FloorPlan.Name, IsUnassigned: false);

            return (Name: NoSectionName, IsUnassigned: true);
        });

        var sections = new List<SectionSalesDto>();

        foreach (var group in grouped)
        {
            var groupOrders = group.ToList();
            var netSales    = groupOrders.Sum(o => o.TotalAmount);
            var orderCount  = groupOrders.Count;

            sections.Add(new SectionSalesDto
            {
                SectionName  = group.Key.Name,
                IsUnassigned = group.Key.IsUnassigned,
                OrderCount   = orderCount,
                NetSales     = netSales,
                TaxCollected = groupOrders.Sum(o => o.TaxAmount),
                AvgTicket    = orderCount > 0
                    ? Math.Round(netSales / orderCount, 2)
                    : 0m,
                TopItems = groupOrders
                    .SelectMany(o => o.OrderLines)
                    .GroupBy(ol => new { ol.MenuItemId, ol.MenuItem.Name, ol.MenuItem.NameAr })
                    .Select(g => new SectionTopItemDto
                    {
                        MenuItemId = g.Key.MenuItemId,
                        Name       = g.Key.Name,
                        NameAr     = g.Key.NameAr,
                        Qty        = (int)g.Sum(ol => ol.Quantity),
                        Sales      = g.Sum(ol => ol.TotalPrice)
                    })
                    .OrderByDescending(i => i.Sales)
                    .Take(5)
                    .ToList(),
                HourlySales = groupOrders
                    .GroupBy(o => o.CreatedAt.Hour)
                    .Select(g => new SectionHourlyDto
                    {
                        Hour   = g.Key,
                        Orders = g.Count(),
                        Sales  = g.Sum(o => o.TotalAmount)
                    })
                    .Where(h => h.Orders > 0)
                    .OrderBy(h => h.Hour)
                    .ToList()
            });
        }

        var totalNetSales = sections.Sum(s => s.NetSales);

        foreach (var section in sections)
        {
            section.SharePercent = totalNetSales > 0
                ? Math.Round(section.NetSales / totalNetSales * 100m, 1)
                : 0m;
        }

        sections = sections
            .Where(s => !s.IsUnassigned)
            .OrderByDescending(s => s.NetSales)
            .Concat(sections.Where(s => s.IsUnassigned))
            .ToList();

        return new SectionSalesReportDto
        {
            From          = request.From,
            To            = request.To,
            TotalOrders   = sections.Sum(s => s.OrderCount),
            TotalNetSales = totalNetSales,
            Sections      = sections
        };
    }
}
