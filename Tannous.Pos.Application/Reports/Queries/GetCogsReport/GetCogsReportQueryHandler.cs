using MediatR;
using Tannous.Pos.Application.DTOs.Reports;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Reports.Queries.GetCogsReport;

public class GetCogsReportQueryHandler : IRequestHandler<GetCogsReportQuery, CogsReportDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryRepository _inventoryRepository;

    public GetCogsReportQueryHandler(IOrderRepository orderRepository, IInventoryRepository inventoryRepository)
    {
        _orderRepository = orderRepository;
        _inventoryRepository = inventoryRepository;
    }

    public async Task<CogsReportDto> Handle(GetCogsReportQuery request, CancellationToken cancellationToken)
    {
        // Get all paid orders in the date range
        var orders = await _orderRepository.GetPaidOrdersInDateRangeAsync(request.From, request.To);
        
        var salesTotal = orders.Sum(o => o.TotalAmount);
        var ingredientUsage = new Dictionary<Guid, (string Name, decimal QtyUsed, decimal Cost)>();

        foreach (var order in orders)
        {
            foreach (var orderLine in order.OrderLines)
            {
                var menuItem = orderLine.MenuItem;
                if (menuItem.Recipes != null && menuItem.Recipes.Any())
                {
                    var recipe = menuItem.Recipes.First(); // Assuming one recipe per menu item
                    
                    foreach (var recipeLine in recipe.RecipeLines)
                    {
                        var ingredientId = recipeLine.IngredientId;
                        var ingredientName = recipeLine.Ingredient.Name;
                        var qtyPerItem = recipeLine.QuantityPerItem;
                        var totalQtyUsed = qtyPerItem * orderLine.Quantity;

                        var inventoryItem = await _inventoryRepository.GetByIngredientAsync(ingredientId);
                        var avgCost = inventoryItem?.AverageCost ?? 0;
                        var lineCost = totalQtyUsed * avgCost;

                        if (ingredientUsage.TryGetValue(ingredientId, out var existing))
                        {
                            ingredientUsage[ingredientId] = (
                                existing.Name,
                                existing.QtyUsed + totalQtyUsed,
                                existing.Cost + lineCost);
                        }
                        else
                        {
                            ingredientUsage[ingredientId] = (ingredientName, totalQtyUsed, lineCost);
                        }
                    }
                }
            }
        }

        var cogsTotal = ingredientUsage.Values.Sum(x => x.Cost);
        var grossMargin = salesTotal - cogsTotal;

        return new CogsReportDto
        {
            From = request.From,
            To = request.To,
            SalesTotal = salesTotal,
            CogsTotal = cogsTotal,
            GrossMargin = grossMargin,
            IngredientUsage = ingredientUsage.Select(kvp => new CogsItemDto
            {
                IngredientId = kvp.Key,
                Name = kvp.Value.Name,
                QtyUsed = kvp.Value.QtyUsed,
                Cost = kvp.Value.Cost
            }).ToList()
        };
    }
}
