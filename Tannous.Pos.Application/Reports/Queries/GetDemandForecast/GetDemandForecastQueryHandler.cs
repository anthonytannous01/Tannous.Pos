using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Application.DTOs.Reports;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Reports.Queries.GetDemandForecast;

public class GetDemandForecastQueryHandler : IRequestHandler<GetDemandForecastQuery, DemandForecastDto>
{
    private const int LookbackDays = 28;
    private const int BlockSizeHours = 3;

    private readonly DbContext _dbContext;

    public GetDemandForecastQueryHandler(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DemandForecastDto> Handle(GetDemandForecastQuery request, CancellationToken cancellationToken)
    {
        var targetDate = (request.TargetDate ?? DateTime.UtcNow.Date.AddDays(1)).Date;
        var targetDow  = targetDate.DayOfWeek;

        var lookbackStart = targetDate.AddDays(-LookbackDays);
        var lookbackEnd   = targetDate;          // exclusive — excludes the target date itself

        var result = new DemandForecastDto
        {
            TargetDate    = targetDate,
            DayOfWeekName = targetDow.ToString()
        };

        // ── Step 1 — Load historical paid orders, same day-of-week, 4-week lookback ──
        var query = _dbContext.Set<Order>()
            .Include(o => o.OrderLines)
                .ThenInclude(ol => ol.MenuItem)
            .Where(o => o.Status == OrderStatus.Paid
                && o.CreatedAt >= lookbackStart
                && o.CreatedAt < lookbackEnd);

        if (request.BranchId.HasValue)
            query = query.Where(o => o.BranchId == request.BranchId.Value);

        var orders = (await query.ToListAsync(cancellationToken))
            .Where(o => o.CreatedAt.DayOfWeek == targetDow)
            .ToList();

        // Same-DOW filter means each calendar week contributes at most one distinct date.
        var weeksWithData = orders.Select(o => o.CreatedAt.Date).Distinct().Count();
        result.WeeksOfDataUsed = Math.Min(weeksWithData, 4);

        if (result.WeeksOfDataUsed == 0)
        {
            result.InsufficientDataMessage =
                "Not enough data yet. Come back after your first full week of trading.";
            return result;
        }

        result.Confidence = result.WeeksOfDataUsed switch
        {
            1     => "Low",
            2 or 3 => "Medium",
            _     => "High"
        };

        var weeks = result.WeeksOfDataUsed;

        // ── Step 2 — Time blocks (8 × 3h), peak flagged ──────────────────────────
        var blocks = new List<TimeBlockForecastDto>();
        for (var startHour = 0; startHour < 24; startHour += BlockSizeHours)
        {
            var endHour = startHour + BlockSizeHours;
            var blockOrders = orders
                .Where(o => o.CreatedAt.Hour >= startHour && o.CreatedAt.Hour < endHour)
                .ToList();

            blocks.Add(new TimeBlockForecastDto
            {
                StartHour       = startHour,
                Label           = $"{startHour:00}:00 – {endHour:00}:00",
                EstimatedOrders = (int)Math.Round((decimal)blockOrders.Count / weeks, 0, MidpointRounding.AwayFromZero),
                EstimatedSales  = Math.Round(blockOrders.Sum(o => o.TotalAmount) / weeks, 2)
            });
        }

        var peakBlock = blocks.OrderByDescending(b => b.EstimatedOrders).First();
        if (peakBlock.EstimatedOrders > 0)
            peakBlock.IsPeakBlock = true;

        result.TimeBlocks = blocks.Where(b => b.EstimatedOrders > 0).ToList();

        // ── Step 5 — Totals (from blocks) ────────────────────────────────────────
        result.EstimatedOrders  = result.TimeBlocks.Sum(b => b.EstimatedOrders);
        result.EstimatedRevenue = Math.Round(result.TimeBlocks.Sum(b => b.EstimatedSales), 2);

        // ── Step 3 — Top items forecast ──────────────────────────────────────────
        result.TopItems = orders
            .SelectMany(o => o.OrderLines)
            .GroupBy(ol => new { ol.MenuItemId, ol.MenuItem.Name, ol.MenuItem.NameAr })
            .Select(g =>
            {
                var avgQty = g.Sum(ol => ol.Quantity) / weeks;
                return new ItemForecastDto
                {
                    MenuItemId   = g.Key.MenuItemId,
                    Name         = g.Key.Name,
                    NameAr       = g.Key.NameAr,
                    AvgQty       = Math.Round(avgQty, 2),
                    EstimatedQty = Math.Round(avgQty * 2, 0, MidpointRounding.AwayFromZero) / 2.0m
                };
            })
            .OrderByDescending(i => i.AvgQty)
            .Take(10)
            .ToList();

        // ── Step 4 — Ingredient demand via recipes ───────────────────────────────
        var topItemIds = result.TopItems.Select(i => i.MenuItemId).ToList();

        var recipes = await _dbContext.Set<Recipe>()
            .Include(r => r.RecipeLines)
                .ThenInclude(rl => rl.Ingredient)
            .Where(r => r.IsActive && topItemIds.Contains(r.MenuItemId))
            .ToListAsync(cancellationToken);

        var demands = new Dictionary<Guid, IngredientDemandDto>();

        foreach (var item in result.TopItems)
        {
            // One recipe per menu item, matching the COGS report convention.
            var recipe = recipes.FirstOrDefault(r => r.MenuItemId == item.MenuItemId);
            if (recipe == null) continue;

            foreach (var line in recipe.RecipeLines)
            {
                var qty = item.EstimatedQty * line.QuantityPerItem;
                if (demands.TryGetValue(line.IngredientId, out var existing))
                {
                    existing.EstimatedQty += qty;
                }
                else
                {
                    demands[line.IngredientId] = new IngredientDemandDto
                    {
                        IngredientId = line.IngredientId,
                        Name         = line.Ingredient.Name,
                        NameAr       = null,   // Ingredient entity has no Arabic name yet
                        Unit         = line.Ingredient.Unit,
                        EstimatedQty = qty
                    };
                }
            }
        }

        result.IngredientDemands = demands.Values
            .Select(d => { d.EstimatedQty = Math.Round(d.EstimatedQty, 2); return d; })
            .Where(d => d.EstimatedQty > 0)
            .OrderByDescending(d => d.EstimatedQty)
            .ToList();

        return result;
    }
}
