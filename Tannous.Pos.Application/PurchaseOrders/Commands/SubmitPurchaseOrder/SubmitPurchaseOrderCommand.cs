using MediatR;
using Tannous.Pos.Application.DTOs.Suppliers;

namespace Tannous.Pos.Application.PurchaseOrders.Commands.SubmitPurchaseOrder;

public class SubmitPurchaseOrderCommand : IRequest<PurchaseOrderDto>
{
    public Guid Id { get; set; }
}
