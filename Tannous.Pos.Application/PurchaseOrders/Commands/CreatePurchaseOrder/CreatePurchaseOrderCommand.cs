using MediatR;
using Tannous.Pos.Application.DTOs.Suppliers;

namespace Tannous.Pos.Application.PurchaseOrders.Commands.CreatePurchaseOrder;

public class CreatePurchaseOrderCommand : IRequest<PurchaseOrderDto>
{
    public CreatePurchaseOrderDto PurchaseOrder { get; set; } = new();
}
