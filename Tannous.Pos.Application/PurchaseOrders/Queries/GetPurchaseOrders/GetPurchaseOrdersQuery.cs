using MediatR;
using Tannous.Pos.Application.DTOs.Suppliers;

namespace Tannous.Pos.Application.PurchaseOrders.Queries.GetPurchaseOrders;

public class GetPurchaseOrdersQuery : IRequest<IEnumerable<PurchaseOrderDto>>
{
}
