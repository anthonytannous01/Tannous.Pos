using MediatR;
using Tannous.Pos.Application.DTOs.Sync;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Inventory.Commands.RecordWastage;

public class RecordWastageCommandHandler : IRequestHandler<RecordWastageCommand, OpResultDto>
{
    private readonly IInventoryRepository _inventoryRepository;

    public RecordWastageCommandHandler(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public async Task<OpResultDto> Handle(
        RecordWastageCommand command, CancellationToken cancellationToken)
    {
        // GetByIngredientAsync does NOT use AsNoTracking — entity is tracked.
        // CurrentStock mutation + AddWastageAsync + AddMovementAsync all persist via CommitAsync.
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

        var wastage = new WastageRecord
        {
            InventoryItemId = inventoryItem.Id,
            Quantity        = command.Quantity,
            Reason          = command.Reason,
            UnitCost        = inventoryItem.AverageCost,
            TotalCost       = command.Quantity * inventoryItem.AverageCost,
            WastageDate     = DateTime.UtcNow
        };
        await _inventoryRepository.AddWastageAsync(wastage);

        inventoryItem.CurrentStock -= command.Quantity;
        inventoryItem.LastUpdated   = DateTime.UtcNow;

        var movement = new InventoryMovement
        {
            IngredientId    = command.IngredientId,
            InventoryItemId = inventoryItem.Id,
            MovementType    = InventoryMovementType.Wastage,
            Quantity        = -command.Quantity,
            UnitCost        = inventoryItem.AverageCost,
            TotalCost       = -command.Quantity * inventoryItem.AverageCost,
            Reference       = $"Wastage: {command.Reason}",
            MovementDate    = DateTime.UtcNow
        };
        await _inventoryRepository.AddMovementAsync(movement);
        await _inventoryRepository.CommitAsync(cancellationToken);

        return new OpResultDto
        {
            OpId     = command.OpId,
            Success  = true,
            ServerId = wastage.Id.ToString(),
            Message  = "Wastage recorded successfully"
        };
    }
}
