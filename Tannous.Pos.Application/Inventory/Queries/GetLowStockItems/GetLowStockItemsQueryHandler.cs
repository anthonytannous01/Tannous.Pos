using MediatR;
using Tannous.Pos.Application.DTOs.Inventory;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Inventory.Queries.GetLowStockItems;

public class GetLowStockItemsQueryHandler
    : IRequestHandler<GetLowStockItemsQuery, IEnumerable<InventoryItemDto>>
{
    private readonly IInventoryRepository _inventoryRepository;

    public GetLowStockItemsQueryHandler(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public async Task<IEnumerable<InventoryItemDto>> Handle(
        GetLowStockItemsQuery query, CancellationToken cancellationToken)
    {
        // GetLowStockItemsAsync already includes Ingredient — no new repository method needed
        var items = await _inventoryRepository.GetLowStockItemsAsync();
        return items.Select(MapToDto).ToList();
    }

    private static InventoryItemDto MapToDto(InventoryItem ii) => new()
    {
        Id             = ii.Id,
        CurrentStock   = ii.CurrentStock,
        MinimumStock   = ii.MinimumStock,
        MaximumStock   = ii.MaximumStock,
        AverageCost    = ii.AverageCost,
        LastUpdated    = ii.LastUpdated,
        IngredientId   = ii.IngredientId,
        IngredientName = ii.Ingredient.Name,
        IngredientUnit = ii.Ingredient.Unit,
        CreatedAt      = ii.CreatedAt
    };
}
