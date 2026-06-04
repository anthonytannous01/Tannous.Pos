using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Application.DTOs.Reports;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Reports.Queries.GetMenuEngineering;

public class GetMenuEngineeringQueryHandler : IRequestHandler<GetMenuEngineeringQuery, MenuEngineeringReportDto>
{
    private readonly DbContext _dbContext;

    public GetMenuEngineeringQueryHandler(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MenuEngineeringReportDto> Handle(
        GetMenuEngineeringQuery request, CancellationToken cancellationToken)
    {
        // Load paid order lines in range with menu item, recipe, and ingredient cost data
        var orderLines = await _dbContext.Set<OrderLine>()
            .Include(ol => ol.Order)
            .Include(ol => ol.MenuItem)
                .ThenInclude(mi => mi.Category)
            .Include(ol => ol.MenuItem)
                .ThenInclude(mi => mi.Recipes.Where(r => r.IsActive))
                    .ThenInclude(r => r.RecipeLines)
                        .ThenInclude(rl => rl.Ingredient)
            .Where(ol =>
                ol.Order.Status == OrderStatus.Paid &&
                ol.Order.CreatedAt >= request.From &&
                ol.Order.CreatedAt < request.To)
            .ToListAsync(cancellationToken);

        // Load average costs for ingredients (for COGS calculation)
        var ingredientIds = orderLines
            .SelectMany(ol => ol.MenuItem.Recipes)
            .SelectMany(r => r.RecipeLines)
            .Select(rl => rl.IngredientId)
            .Distinct()
            .ToList();

        var inventoryItems = await _dbContext.Set<InventoryItem>()
            .Where(ii => ingredientIds.Contains(ii.IngredientId))
            .ToDictionaryAsync(ii => ii.IngredientId, ii => ii.AverageCost, cancellationToken);

        var totalOrders = orderLines.Select(ol => ol.OrderId).Distinct().Count();

        // Aggregate per menu item
        var grouped = orderLines
            .GroupBy(ol => ol.MenuItemId)
            .Select(g =>
            {
                var sample    = g.First().MenuItem;
                var unitsSold = (int)g.Sum(ol => ol.Quantity);
                var revenue   = g.Sum(ol => ol.TotalPrice);

                // Compute COGS using first active recipe
                var recipe = sample.Recipes.FirstOrDefault();
                decimal cogsPerUnit = 0m;
                if (recipe != null)
                {
                    cogsPerUnit = recipe.RecipeLines.Sum(rl =>
                        rl.QuantityPerItem *
                        (inventoryItems.TryGetValue(rl.IngredientId, out var cost) ? cost : 0m));
                }

                var totalCogs = cogsPerUnit * unitsSold;
                var revenuePerUnit = unitsSold > 0 ? revenue / unitsSold : sample.Price;
                var cm = revenuePerUnit - cogsPerUnit;
                var cmPct = revenuePerUnit > 0 ? Math.Round(cm / revenuePerUnit * 100, 1) : 0m;

                return new
                {
                    MenuItemId   = g.Key,
                    Name         = sample.Name,
                    CategoryName = sample.Category?.Name ?? string.Empty,
                    UnitsSold    = unitsSold,
                    Revenue      = revenue,
                    CostOfGoods  = totalCogs,
                    CmPerUnit    = cm,
                    CmPct        = cmPct
                };
            })
            .ToList();

        if (!grouped.Any())
        {
            return new MenuEngineeringReportDto { From = request.From, To = request.To, TotalOrders = totalOrders };
        }

        // Thresholds: average popularity and average CM
        var totalUnits       = grouped.Sum(x => x.UnitsSold);
        var avgPopularity    = totalUnits > 0 ? (double)totalUnits / grouped.Count : 0;
        var avgCm            = grouped.Average(x => (double)x.CmPerUnit);

        // 70% rule for popularity (industry standard): item is popular if sold ≥ 70% of avg
        var popularityThreshold = avgPopularity * 0.70;

        var items = grouped.Select(x =>
        {
            var highPop    = x.UnitsSold >= popularityThreshold;
            var highMargin = (double)x.CmPerUnit >= avgCm;
            var popIndex   = totalUnits > 0
                ? Math.Round((decimal)x.UnitsSold / totalUnits * 100, 2)
                : 0m;

            var category = (highPop, highMargin) switch
            {
                (true,  true)  => MenuEngineeringCategory.Star,
                (true,  false) => MenuEngineeringCategory.Plowhorse,
                (false, true)  => MenuEngineeringCategory.Puzzle,
                _              => MenuEngineeringCategory.Dog
            };

            return new MenuEngineeringItemDto
            {
                MenuItemId            = x.MenuItemId,
                Name                  = x.Name,
                CategoryName          = x.CategoryName,
                UnitsSold             = x.UnitsSold,
                PopularityIndex       = popIndex,
                IsHighPopularity      = highPop,
                Revenue               = x.Revenue,
                CostOfGoods           = x.CostOfGoods,
                ContributionMargin    = Math.Round(x.CmPerUnit, 2),
                ContributionMarginPct = x.CmPct,
                IsHighMargin          = highMargin,
                Category              = category
            };
        })
        .OrderBy(x => x.Category)
        .ThenByDescending(x => x.UnitsSold)
        .ToList();

        return new MenuEngineeringReportDto
        {
            From        = request.From,
            To          = request.To,
            TotalOrders = totalOrders,
            Items       = items
        };
    }
}
