using MediatR;
using Tannous.Pos.Application.DTOs.Sync;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Inventory.Commands.AdjustInventory;

public class AdjustInventoryCommandHandler : IRequestHandler<AdjustInventoryCommand, OpResultDto>
{
    private readonly IInventoryRepository _inventoryRepository;

    public AdjustInventoryCommandHandler(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public async Task<OpResultDto> Handle(
        AdjustInventoryCommand command, CancellationToken cancellationToken)
    {
        // GetByIngredientAsync does NOT use AsNoTracking — entity is tracked.
        var inventoryItem = await _inventoryRepository.GetByIngredientAsync(command.IngredientId);
        if (inventoryItem == null)
        {
            return new OpResultDto
            {
                OpId    = command.OpId,
                Success = false,
                Message = "Ingredient not found in inventory"
            };
        }

        inventoryItem.CurrentStock += command.Quantity;   // Quantity may be negative (reduction)
        inventoryItem.LastUpdated   = DateTime.UtcNow;

        var movement = new InventoryMovement
        {
            IngredientId    = command.IngredientId,
            InventoryItemId = inventoryItem.Id,
            MovementType    = InventoryMovementType.Adjustment,
            Quantity        = command.Quantity,
            UnitCost        = inventoryItem.AverageCost,
            TotalCost       = command.Quantity * inventoryItem.AverageCost,
            Reference       = $"Adjustment: {command.Reason}",
            MovementDate    = DateTime.UtcNow
        };
        await _inventoryRepository.AddMovementAsync(movement);
        await _inventoryRepository.CommitAsync(cancellationToken);

        return new OpResultDto
        {
            OpId     = command.OpId,
            Success  = true,
            ServerId = movement.Id.ToString(),
            Message  = "Inventory adjusted successfully"
        };
    }
}
