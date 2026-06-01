using MediatR;
using Tannous.Pos.Application.DTOs.Suppliers;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.PurchaseOrders.Queries.GetPurchaseOrders;

public class GetPurchaseOrdersQueryHandler
    : IRequestHandler<GetPurchaseOrdersQuery, IEnumerable<PurchaseOrderDto>>
{
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;

    public GetPurchaseOrdersQueryHandler(IPurchaseOrderRepository purchaseOrderRepository)
    {
        _purchaseOrderRepository = purchaseOrderRepository;
    }

    public async Task<IEnumerable<PurchaseOrderDto>> Handle(
        GetPurchaseOrdersQuery query, CancellationToken cancellationToken)
    {
        // GetAllAsync already eager-loads Supplier + Lines + Ingredient — no new includes needed
        var purchaseOrders = await _purchaseOrderRepository.GetAllAsync();
        return purchaseOrders.Select(MapToDto).ToList();
    }

    private static PurchaseOrderDto MapToDto(PurchaseOrder po) => new()
    {
        Id                   = po.Id,
        OrderNumber          = po.OrderNumber,
        SupplierId           = po.SupplierId,
        SupplierName         = po.Supplier.Name,
        Status               = po.Status.ToString(),
        OrderDate            = po.OrderDate,
        ExpectedDeliveryDate = po.ExpectedDeliveryDate,
        SubTotal             = po.SubTotal,
        TaxAmount            = po.TaxAmount,
        TotalAmount          = po.TotalAmount,
        Notes                = po.Notes,
        CreatedAt            = po.CreatedAt,
        Lines                = po.Lines.Select(pol => new PurchaseOrderLineDto
        {
            Id             = pol.Id,
            IngredientId   = pol.IngredientId,
            IngredientName = pol.Ingredient.Name,
            Quantity       = pol.Quantity,
            UnitCost       = pol.UnitCost,
            TotalCost      = pol.TotalCost,
            Unit           = pol.Ingredient.Unit
        }).ToList()
    };
}
