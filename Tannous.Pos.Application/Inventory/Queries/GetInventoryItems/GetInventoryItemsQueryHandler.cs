using MediatR;
using Tannous.Pos.Application.DTOs.Inventory;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Inventory.Queries.GetInventoryItems;

public class GetInventoryItemsQueryHandler
    : IRequestHandler<GetInventoryItemsQuery, IEnumerable<InventoryItemDto>>
{
    private readonly IInventoryRepository _inventoryRepository;

    public GetInventoryItemsQueryHandler(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public async Task<IEnumerable<InventoryItemDto>> Handle(
        GetInventoryItemsQuery query, CancellationToken cancellationToken)
    {
        // GetAllWithIngredientAsync includes Ingredient eagerly — required for IngredientName/Unit mapping
        var items = await _inventoryRepository.GetAllWithIngredientAsync();
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
