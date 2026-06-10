using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Application.DTOs.Reports;
using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Application.Kds.Queries.GetKdsPerformance;

public class GetKdsPerformanceQueryHandler : IRequestHandler<GetKdsPerformanceQuery, KdsPerformanceDto>
{
    private readonly DbContext _dbContext;

    public GetKdsPerformanceQueryHandler(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<KdsPerformanceDto> Handle(GetKdsPerformanceQuery request, CancellationToken cancellationToken)
    {
        var result = new KdsPerformanceDto
        {
            From = request.From,
            To   = request.To
        };

        var query = _dbContext.Set<OrderLine>()
            .Include(ol => ol.Order)
            .Include(ol => ol.MenuItem)
            .Where(ol => ol.KdsDoneAt != null
                && ol.KdsDoneAt >= request.From
                && ol.KdsDoneAt < request.To);

        if (request.BranchId.HasValue)
            query = query.Where(ol => ol.Order.BranchId == request.BranchId.Value);

        var lines = await query.ToListAsync(cancellationToken);

        result.TotalTickets = lines.Count;
        if (lines.Count == 0)
            return result;

        // ── Acknowledge times (Order.CreatedAt → KdsAcknowledgedAt) ────────────
        var ackSeconds = lines
            .Where(l => l.KdsAcknowledgedAt != null)
            .Select(l => Math.Max(0, (l.KdsAcknowledgedAt!.Value - l.Order.CreatedAt).TotalSeconds))
            .ToList();

        result.AvgAcknowledgeSeconds = Round1(Average(ackSeconds));
        result.P90AcknowledgeSeconds = Round1(Percentile90(ackSeconds));

        // ── Prep times (KdsAcknowledgedAt → KdsDoneAt) ───────────────────────────
        var prepSeconds = lines
            .Where(l => l.KdsAcknowledgedAt != null)
            .Select(l => Math.Max(0, (l.KdsDoneAt!.Value - l.KdsAcknowledgedAt!.Value).TotalSeconds))
            .ToList();

        result.AvgPrepSeconds = Round1(Average(prepSeconds));
        result.P90PrepSeconds = Round1(Percentile90(prepSeconds));

        // ── Total ticket times (Order.CreatedAt → KdsDoneAt) ───────────────────
        var totalSeconds = lines
            .Select(l => Math.Max(0, (l.KdsDoneAt!.Value - l.Order.CreatedAt).TotalSeconds))
            .ToList();

        result.AvgTotalTicketSeconds = Round1(Average(totalSeconds));
        result.P90TotalTicketSeconds = Round1(Percentile90(totalSeconds));

        // ── Throughput by hour of completion ───────────────────────────────────
        var hourlyCounts = lines
            .GroupBy(l => l.KdsDoneAt!.Value.Hour)
            .ToDictionary(g => g.Key, g => g.Count());

        if (hourlyCounts.Count > 0)
        {
            result.AvgThroughputPerHour = Round1(hourlyCounts.Values.Average());
            var peak = hourlyCounts.OrderByDescending(kv => kv.Value).First();
            result.PeakThroughputHour  = peak.Key;
            result.PeakThroughputCount = peak.Value;
        }

        // ── Hourly breakdown ───────────────────────────────────────────────────
        result.HourlyBreakdown = lines
            .GroupBy(l => l.KdsDoneAt!.Value.Hour)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var hourTotals = g
                    .Select(l => Math.Max(0, (l.KdsDoneAt!.Value - l.Order.CreatedAt).TotalSeconds))
                    .ToList();
                return new KdsHourlyDto
                {
                    Hour                  = g.Key,
                    TicketsCompleted      = g.Count(),
                    AvgTotalTicketSeconds = Round1(Average(hourTotals))
                };
            })
            .ToList();

        // ── Per-item breakdown (slowest first, top 20) ─────────────────────────
        result.ItemBreakdown = lines
            .Where(l => l.KdsAcknowledgedAt != null)
            .GroupBy(l => new { l.MenuItemId, l.MenuItem.Name, l.MenuItem.NameAr })
            .Select(g =>
            {
                var itemPrep = g
                    .Select(l => Math.Max(0, (l.KdsDoneAt!.Value - l.KdsAcknowledgedAt!.Value).TotalSeconds))
                    .ToList();
                return new KdsItemPerformanceDto
                {
                    MenuItemId    = g.Key.MenuItemId,
                    Name          = g.Key.Name,
                    NameAr        = g.Key.NameAr,
                    TicketCount   = g.Count(),
                    AvgPrepSeconds = Round1(Average(itemPrep)),
                    P90PrepSeconds = Round1(Percentile90(itemPrep))
                };
            })
            .OrderByDescending(i => i.AvgPrepSeconds)
            .Take(20)
            .ToList();

        return result;
    }

    private static double Average(IReadOnlyList<double> values) =>
        values.Count == 0 ? 0 : values.Average();

    /// <summary>P90: sort ascending, index = Ceil(0.9 * n) - 1.</summary>
    private static double Percentile90(IReadOnlyList<double> values)
    {
        if (values.Count == 0) return 0;
        var sorted = values.OrderBy(v => v).ToList();
        var index = (int)Math.Ceiling(0.9 * sorted.Count) - 1;
        if (index < 0) index = 0;
        if (index >= sorted.Count) index = sorted.Count - 1;
        return sorted[index];
    }

    private static double Round1(double value) => Math.Round(value, 1);
}
