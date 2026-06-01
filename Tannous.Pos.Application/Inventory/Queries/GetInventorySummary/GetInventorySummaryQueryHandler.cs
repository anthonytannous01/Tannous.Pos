using MediatR;
using Tannous.Pos.Application.DTOs.Inventory;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Inventory.Queries.GetInventorySummary;

public class GetInventorySummaryQueryHandler
    : IRequestHandler<GetInventorySummaryQuery, IEnumerable<InventorySummaryDto>>
{
    private readonly IInventoryRepository _inventoryRepository;

    public GetInventorySummaryQueryHandler(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public async Task<IEnumerable<InventorySummaryDto>> Handle(
        GetInventorySummaryQuery query, CancellationToken cancellationToken)
    {
        // Reuses GetAllWithIngredientAsync — same data as GetInventoryItems, different projection.
        // Original used a server-side EF Select; this is functionally equivalent in-memory projection.
        var items = await _inventoryRepository.GetAllWithIngredientAsync();
        return items.Select(ii => new InventorySummaryDto
        {
            IngredientId   = ii.IngredientId,
            IngredientName = ii.Ingredient.Name,
            OnHand         = ii.CurrentStock
        }).ToList();
    }
}
