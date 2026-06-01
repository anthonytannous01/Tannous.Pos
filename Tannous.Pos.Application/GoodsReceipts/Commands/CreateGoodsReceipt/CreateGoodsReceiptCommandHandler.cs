using MediatR;
using Tannous.Pos.Application.DTOs.Suppliers;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.GoodsReceipts.Commands.CreateGoodsReceipt;

public class CreateGoodsReceiptCommandHandler : IRequestHandler<CreateGoodsReceiptCommand, GoodsReceiptDto>
{
    private readonly IGoodsReceiptRepository _goodsReceiptRepository;
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly IIngredientRepository _ingredientRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateGoodsReceiptCommandHandler(
        IGoodsReceiptRepository goodsReceiptRepository,
        IPurchaseOrderRepository purchaseOrderRepository,
        IIngredientRepository ingredientRepository,
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork)
    {
        _goodsReceiptRepository = goodsReceiptRepository;
        _purchaseOrderRepository = purchaseOrderRepository;
        _ingredientRepository = ingredientRepository;
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<GoodsReceiptDto> Handle(CreateGoodsReceiptCommand request, CancellationToken cancellationToken)
    {
        // Validate purchase order if provided
        PurchaseOrder? purchaseOrder = null;
        if (request.GoodsReceipt.PurchaseOrderId.HasValue)
        {
            purchaseOrder = await _purchaseOrderRepository.GetByIdWithDetailsAsync(request.GoodsReceipt.PurchaseOrderId.Value);
            if (purchaseOrder == null)
                throw new ArgumentException($"Purchase order with ID {request.GoodsReceipt.PurchaseOrderId} not found");
        }

        // Validate all ingredients exist
        foreach (var line in request.GoodsReceipt.Lines)
        {
            var ingredient = await _ingredientRepository.GetByIdAsync(line.IngredientId);
            if (ingredient == null)
                throw new ArgumentException($"Ingredient with ID {line.IngredientId} not found");
        }

        var goodsReceipt = new GoodsReceipt
        {
            ReceiptNumber = GenerateReceiptNumber(),
            PurchaseOrderId = request.GoodsReceipt.PurchaseOrderId,
            ReceiptDate = request.GoodsReceipt.ReceiptDate ?? DateTime.UtcNow,
            Notes = request.GoodsReceipt.Notes
        };

        // Create goods receipt lines and update inventory
        foreach (var lineDto in request.GoodsReceipt.Lines)
        {
            var line = new GoodsReceiptLine
            {
                GoodsReceiptId = goodsReceipt.Id,
                IngredientId = lineDto.IngredientId,
                Quantity = lineDto.Quantity,
                UnitCost = lineDto.UnitCost,
                Unit = "pcs" // Default unit
            };
            goodsReceipt.Lines.Add(line);

            // Update inventory with moving-average cost
            await UpdateInventoryWithMovingAverage(lineDto.IngredientId, lineDto.Quantity, lineDto.UnitCost);
        }

        await _goodsReceiptRepository.AddAsync(goodsReceipt);
        await _unitOfWork.SaveChangesAsync();

        // Return goods receipt with details
        return new GoodsReceiptDto
        {
            Id = goodsReceipt.Id,
            ReceiptNumber = goodsReceipt.ReceiptNumber,
            PurchaseOrderId = goodsReceipt.PurchaseOrderId,
            PurchaseOrderNumber = purchaseOrder?.OrderNumber,
            ReceiptDate = goodsReceipt.ReceiptDate,
            Notes = goodsReceipt.Notes,
            CreatedAt = goodsReceipt.CreatedAt,
            Lines = goodsReceipt.Lines.Select(l => new GoodsReceiptLineDto
            {
                Id = l.Id,
                IngredientId = l.IngredientId,
                IngredientName = l.Ingredient?.Name ?? string.Empty,
                Quantity = l.Quantity,
                UnitCost = l.UnitCost,
                TotalCost = l.Quantity * l.UnitCost,
                Unit = l.Unit
            }).ToList()
        };
    }

    private async Task UpdateInventoryWithMovingAverage(Guid ingredientId, decimal receivedQuantity, decimal unitCost)
    {
        var inventoryItem = await _inventoryRepository.GetByIngredientAsync(ingredientId);
        
        if (inventoryItem == null)
        {
            // Create new inventory item
            inventoryItem = new InventoryItem
            {
                IngredientId = ingredientId,
                CurrentStock = receivedQuantity,
                AverageCost = unitCost,
                Unit = "pcs"
            };
            await _inventoryRepository.AddAsync(inventoryItem);
        }
        else
        {
            // Update existing inventory with moving-average cost
            var oldQuantity = inventoryItem.CurrentStock;
            var oldAverageCost = inventoryItem.AverageCost;
            
            var newQuantity = oldQuantity + receivedQuantity;
            var newAverageCost = (oldQuantity * oldAverageCost + receivedQuantity * unitCost) / newQuantity;
            
            inventoryItem.CurrentStock = newQuantity;
            inventoryItem.AverageCost = newAverageCost;
            
            await _inventoryRepository.UpdateAsync(inventoryItem);
        }

        // Create inventory movement record
        var movement = new InventoryMovement
        {
            IngredientId = ingredientId,
            MovementType = InventoryMovementType.Purchase,
            Quantity = receivedQuantity,
            UnitCost = unitCost,
            Reference = $"GR-{ingredientId}",
            Notes = "Goods receipt"
        };
        await _inventoryRepository.AddMovementAsync(movement);
    }

    private string GenerateReceiptNumber()
    {
        return $"GR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
    }
}
