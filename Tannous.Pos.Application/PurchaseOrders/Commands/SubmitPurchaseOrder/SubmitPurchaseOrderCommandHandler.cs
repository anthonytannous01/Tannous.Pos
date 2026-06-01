using MediatR;
using Tannous.Pos.Application.DTOs.Suppliers;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.PurchaseOrders.Commands.SubmitPurchaseOrder;

public class SubmitPurchaseOrderCommandHandler : IRequestHandler<SubmitPurchaseOrderCommand, PurchaseOrderDto>
{
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SubmitPurchaseOrderCommandHandler(
        IPurchaseOrderRepository purchaseOrderRepository,
        IUnitOfWork unitOfWork)
    {
        _purchaseOrderRepository = purchaseOrderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PurchaseOrderDto> Handle(SubmitPurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var purchaseOrder = await _purchaseOrderRepository.GetByIdWithDetailsAsync(request.Id);
        if (purchaseOrder == null)
            throw new ArgumentException($"Purchase order with ID {request.Id} not found");

        if (purchaseOrder.Status != "Draft")
            throw new InvalidOperationException($"Cannot submit purchase order with status '{purchaseOrder.Status}'. Only Draft orders can be submitted.");

        // Change status to Submitted
        purchaseOrder.Status = "Submitted";
        purchaseOrder.UpdatedAt = DateTime.UtcNow;

        await _purchaseOrderRepository.UpdateAsync(purchaseOrder);
        await _unitOfWork.SaveChangesAsync();

        // Return updated purchase order
        return new PurchaseOrderDto
        {
            Id = purchaseOrder.Id,
            OrderNumber = purchaseOrder.OrderNumber,
            SupplierId = purchaseOrder.SupplierId,
            SupplierName = purchaseOrder.Supplier?.Name ?? string.Empty,
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
}
