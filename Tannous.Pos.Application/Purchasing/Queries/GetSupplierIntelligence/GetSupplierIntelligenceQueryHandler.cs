using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Application.DTOs.Purchasing;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Purchasing.Queries.GetSupplierIntelligence;

public class GetSupplierIntelligenceQueryHandler
    : IRequestHandler<GetSupplierIntelligenceQuery, SupplierIntelligenceDto>
{
    private const int LookbackDays = 28;

    private readonly DbContext _dbContext;

    public GetSupplierIntelligenceQueryHandler(DbContext dbContext) => _dbContext = dbContext;

    public async Task<SupplierIntelligenceDto> Handle(
        GetSupplierIntelligenceQuery request, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;

        var ingredients = await _dbContext.Set<Ingredient>()
            .AsNoTracking()
            .Include(i => i.RecipeLines)
                .ThenInclude(rl => rl.Recipe)
                    .ThenInclude(r => r.MenuItem)
            .Include(i => i.PreferredSupplier)
            .Where(i => i.IsActive)
            .ToListAsync(cancellationToken);

        var lookbackStart = today.AddDays(-LookbackDays);
        var recentOrdersQuery = _dbContext.Set<Order>()
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Paid && o.CreatedAt >= lookbackStart);

        if (request.BranchId.HasValue)
            recentOrdersQuery = recentOrdersQuery.Where(o => o.BranchId == request.BranchId);

        var recentOrders = await recentOrdersQuery.ToListAsync(cancellationToken);

        var weeksWithData = recentOrders
            .Select(o => ISOWeek.GetYear(o.CreatedAt) * 100 + ISOWeek.GetWeekOfYear(o.CreatedAt))
            .Distinct()
            .Count();

        var confidence = weeksWithData switch
        {
            0 or 1 => "Low",
            2 or 3 => "Medium",
            _      => "High"
        };

        var recipes = await _dbContext.Set<Recipe>()
            .AsNoTracking()
            .Include(r => r.RecipeLines)
            .Include(r => r.MenuItem)
            .Where(r => r.IsActive && r.MenuItem.IsActive)
            .ToListAsync(cancellationToken);

        var projectedUsage = new Dictionary<Guid, decimal>();

        for (var dayOffset = 1; dayOffset <= request.ForecastDays; dayOffset++)
        {
            var targetDate      = today.AddDays(dayOffset);
            var targetDow       = targetDate.DayOfWeek;
            var dayLookbackStart = targetDate.AddDays(-LookbackDays);
            var dayLookbackEnd   = targetDate;

            var dayOrdersQuery = _dbContext.Set<Order>()
                .AsNoTracking()
                .Include(o => o.OrderLines)
                .Where(o => o.Status == OrderStatus.Paid
                    && o.CreatedAt >= dayLookbackStart
                    && o.CreatedAt < dayLookbackEnd);

            if (request.BranchId.HasValue)
                dayOrdersQuery = dayOrdersQuery.Where(o => o.BranchId == request.BranchId);

            var dayOrders = (await dayOrdersQuery.ToListAsync(cancellationToken))
                .Where(o => o.CreatedAt.DayOfWeek == targetDow)
                .ToList();

            var weeksForDay = Math.Min(dayOrders.Select(o => o.CreatedAt.Date).Distinct().Count(), 4);
            if (weeksForDay == 0)
                continue;

            var menuItemAvgQty = dayOrders
                .SelectMany(o => o.OrderLines)
                .GroupBy(ol => ol.MenuItemId)
                .ToDictionary(g => g.Key, g => g.Sum(ol => ol.Quantity) / (decimal)weeksForDay);

            foreach (var recipe in recipes)
            {
                if (!menuItemAvgQty.TryGetValue(recipe.MenuItemId, out var avgQty) || avgQty <= 0)
                    continue;

                foreach (var line in recipe.RecipeLines)
                {
                    if (!projectedUsage.ContainsKey(line.IngredientId))
                        projectedUsage[line.IngredientId] = 0;

                    projectedUsage[line.IngredientId] += avgQty * line.QuantityPerItem;
                }
            }
        }

        var lineSuggestions = new List<OrderLineSuggestionDto>();
        var lowStockAlerts  = new List<LowStockAlertDto>();

        foreach (var ingredient in ingredients)
        {
            if (ingredient.CurrentStock < ingredient.MinimumStock)
            {
                lowStockAlerts.Add(new LowStockAlertDto
                {
                    IngredientId = ingredient.Id,
                    Name         = ingredient.Name,
                    Unit         = ingredient.Unit,
                    CurrentStock = ingredient.CurrentStock,
                    MinimumStock = ingredient.MinimumStock,
                    Deficit      = ingredient.MinimumStock - ingredient.CurrentStock,
                    SupplierName = ingredient.PreferredSupplier?.Name
                });
            }

            projectedUsage.TryGetValue(ingredient.Id, out var projected);
            var suggestedQty = Math.Max(0, projected + ingredient.MinimumStock - ingredient.CurrentStock);
            suggestedQty = Math.Round(suggestedQty, 1);

            if (suggestedQty <= 0 && ingredient.CurrentStock >= ingredient.MinimumStock)
                continue;

            var isLowStock = ingredient.CurrentStock < ingredient.MinimumStock;
            lineSuggestions.Add(new OrderLineSuggestionDto
            {
                IngredientId   = ingredient.Id,
                Name           = ingredient.Name,
                Unit           = ingredient.Unit,
                CurrentStock   = ingredient.CurrentStock,
                MinimumStock   = ingredient.MinimumStock,
                ProjectedUsage = Math.Round(projected, 1),
                SuggestedQty   = suggestedQty,
                UnitCost       = ingredient.CostPerUnit,
                EstimatedCost  = Math.Round(suggestedQty * ingredient.CostPerUnit, 2),
                IsLowStock     = isLowStock
            });
        }

        var orderSuggestions = lineSuggestions
            .GroupBy(line =>
            {
                var ingredient = ingredients.First(i => i.Id == line.IngredientId);
                return ingredient.PreferredSupplierId;
            })
            .Select(g =>
            {
                var firstIngredient = ingredients.First(i => i.Id == g.First().IngredientId);
                var supplier        = firstIngredient.PreferredSupplier;

                var lines = g
                    .OrderByDescending(l => l.IsLowStock)
                    .ThenBy(l => l.Name)
                    .ToList();

                return new SupplierOrderSuggestionDto
                {
                    SupplierId         = g.Key,
                    SupplierName       = supplier?.Name ?? "Unassigned",
                    SupplierPhone      = supplier?.Phone,
                    SupplierEmail      = supplier?.Email,
                    Lines              = lines,
                    TotalEstimatedCost = lines.Sum(l => l.EstimatedCost)
                };
            })
            .OrderBy(s => s.SupplierId == null)
            .ThenBy(s => s.SupplierName)
            .ToList();

        var suggestedLines = lineSuggestions.Where(l => l.SuggestedQty > 0).ToList();

        return new SupplierIntelligenceDto
        {
            GeneratedAt              = DateTime.UtcNow,
            ForecastDays             = request.ForecastDays,
            Confidence               = confidence,
            LowStockAlerts           = lowStockAlerts.OrderBy(a => a.Name).ToList(),
            OrderSuggestions         = orderSuggestions,
            TotalIngredientsAnalysed = ingredients.Count,
            TotalSuggestedLines      = suggestedLines.Count,
            TotalEstimatedCost       = suggestedLines.Sum(l => l.EstimatedCost)
        };
    }
}
