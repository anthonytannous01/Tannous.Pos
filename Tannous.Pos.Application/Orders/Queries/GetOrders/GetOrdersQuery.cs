using MediatR;
using Tannous.Pos.Application.DTOs.Orders;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Orders.Queries.GetOrders;

public class GetOrdersQuery : IRequest<IEnumerable<OrderDto>>
{
    public OrderStatus? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? ShiftId { get; set; }
    /// <summary>When set, only returns orders belonging to this branch.</summary>
    public Guid? BranchId { get; set; }
}
