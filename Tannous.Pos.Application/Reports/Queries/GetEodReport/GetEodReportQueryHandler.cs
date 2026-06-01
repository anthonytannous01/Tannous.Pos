using MediatR;
using Tannous.Pos.Application.DTOs.Reports;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Reports.Queries.GetEodReport;

public class GetEodReportQueryHandler : IRequestHandler<GetEodReportQuery, EodReportDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IShiftRepository _shiftRepository;
    private readonly ICashDrawerEventRepository _cashDrawerEventRepository;

    public GetEodReportQueryHandler(
        IOrderRepository orderRepository,
        IShiftRepository shiftRepository,
        ICashDrawerEventRepository cashDrawerEventRepository)
    {
        _orderRepository = orderRepository;
        _shiftRepository = shiftRepository;
        _cashDrawerEventRepository = cashDrawerEventRepository;
    }

    public async Task<EodReportDto> Handle(GetEodReportQuery request, CancellationToken cancellationToken)
    {
        var reportDate = (request.Date ?? DateTime.UtcNow).Date;
        var nextDay = reportDate.AddDays(1);

        var orders = (await _orderRepository.GetPaidOrdersInDateRangeAsync(reportDate, nextDay)).ToList();

        var cashDrops = await _cashDrawerEventRepository.GetDropTotalAsync(reportDate, nextDay, cancellationToken);

        var shifts = (await _shiftRepository.GetByDateRangeAsync(reportDate, nextDay))
            .Where(s => s.Status == ShiftStatus.Closed)
            .ToList();

        var netSales = orders.Sum(o => o.TotalAmount);
        var ordersCount = orders.Count;
        var avgTicket = ordersCount > 0 ? netSales / ordersCount : 0m;

        var topItems = orders
            .SelectMany(o => o.OrderLines)
            .GroupBy(ol => new { ol.MenuItemId, ol.MenuItem.Name })
            .Select(g => new TopItemDto
            {
                ItemId = g.Key.MenuItemId,
                Name = g.Key.Name,
                Qty = (int)g.Sum(ol => ol.Quantity),
                Sales = g.Sum(ol => ol.TotalPrice)
            })
            .OrderByDescending(ti => ti.Sales)
            .Take(10)
            .ToList();

        var variance = shifts.Sum(s => s.CashDifference ?? 0m);

        return new EodReportDto
        {
            Date = reportDate,
            NetSales = netSales,
            OrdersCount = ordersCount,
            AvgTicket = avgTicket,
            TopItems = topItems,
            CashDrops = cashDrops,
            Variance = variance
        };
    }
}
