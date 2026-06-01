using MediatR;
using Tannous.Pos.Application.DTOs.Inventory;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Inventory.Queries.GetInventoryItem;

public class GetInventoryItemQueryHandler : IRequestHandler<GetInventoryItemQuery, InventoryItemDto?>
{
    private readonly IInventoryRepository _inventoryRepository;

    public GetInventoryItemQueryHandler(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public async Task<InventoryItemDto?> Handle(
        GetInventoryItemQuery query, CancellationToken cancellationToken)
    {
        // GetByIdWithIngredientAsync includes Ingredient — required for IngredientName/Unit mapping
        var item = await _inventoryRepository.GetByIdWithIngredientAsync(query.Id);
        if (item == null) return null;
        return MapToDto(item);
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
