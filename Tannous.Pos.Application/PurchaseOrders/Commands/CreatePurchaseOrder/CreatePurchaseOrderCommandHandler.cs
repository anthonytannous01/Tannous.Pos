using MediatR;
using Tannous.Pos.Application.DTOs.Suppliers;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.PurchaseOrders.Commands.CreatePurchaseOrder;

public class CreatePurchaseOrderCommandHandler : IRequestHandler<CreatePurchaseOrderCommand, PurchaseOrderDto>
{
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IIngredientRepository _ingredientRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePurchaseOrderCommandHandler(
        IPurchaseOrderRepository purchaseOrderRepository,
        ISupplierRepository supplierRepository,
        IIngredientRepository ingredientRepository,
        IUnitOfWork unitOfWork)
    {
        _purchaseOrderRepository = purchaseOrderRepository;
        _supplierRepository = supplierRepository;
        _ingredientRepository = ingredientRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PurchaseOrderDto> Handle(CreatePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        // Validate supplier exists
        var supplier = await _supplierRepository.GetByIdAsync(request.PurchaseOrder.SupplierId);
        if (supplier == null)
            throw new ArgumentException($"Supplier with ID {request.PurchaseOrder.SupplierId} not found");

        // Validate all ingredients exist
        foreach (var line in request.PurchaseOrder.Lines)
        {
            var ingredient = await _ingredientRepository.GetByIdAsync(line.IngredientId);
            if (ingredient == null)
                throw new ArgumentException($"Ingredient with ID {line.IngredientId} not found");
        }

        var purchaseOrder = new PurchaseOrder
        {
            OrderNumber = GenerateOrderNumber(),
            SupplierId = request.PurchaseOrder.SupplierId,
            Status = "Draft",
            OrderDate = DateTime.UtcNow,
            ExpectedDeliveryDate = request.PurchaseOrder.ExpectedDeliveryDate,
            Notes = request.PurchaseOrder.Notes
        };

        // Create purchase order lines
        foreach (var lineDto in request.PurchaseOrder.Lines)
        {
            var line = new PurchaseOrderLine
            {
                PurchaseOrderId = purchaseOrder.Id,
                IngredientId = lineDto.IngredientId,
                Quantity = lineDto.Quantity,
                UnitCost = lineDto.UnitCost,
                Unit = "pcs" // Default unit
            };
            purchaseOrder.Lines.Add(line);
        }

        // Calculate totals
        purchaseOrder.SubTotal = purchaseOrder.Lines.Sum(l => l.Quantity * l.UnitCost);
        purchaseOrder.TaxAmount = 0; // No tax for now
        purchaseOrder.TotalAmount = purchaseOrder.SubTotal + purchaseOrder.TaxAmount;

        await _purchaseOrderRepository.AddAsync(purchaseOrder);
        await _unitOfWork.SaveChangesAsync();

        // Return purchase order with details
        return new PurchaseOrderDto
        {
            Id = purchaseOrder.Id,
            OrderNumber = purchaseOrder.OrderNumber,
            SupplierId = purchaseOrder.SupplierId,
            SupplierName = supplier.Name,
            Status = purchaseOrder.Status,
            OrderDate = purchaseOrder.OrderDate,
            ExpectedDeliveryDate = purchaseOrder.ExpectedDeliveryDate,
            SubTotal = purchaseOrder.SubTotal,
            TaxAmount = purchaseOrder.TaxAmount,
            TotalAmount = purchaseOrder.TotalAmount,
            Notes = purchaseOrder.Notes,
            CreatedAt = purchaseOrder.CreatedAt,
            Lines = purchaseOrder.Lines.Select(l => new PurchaseOrderLineDto
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

    private string GenerateOrderNumber()
    {
        return $"PO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
    }
}
