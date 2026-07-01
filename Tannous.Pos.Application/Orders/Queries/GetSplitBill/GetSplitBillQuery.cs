using MediatR;
using Tannous.Pos.Application.DTOs.Orders;

namespace Tannous.Pos.Application.Orders.Queries.GetSplitBill;

public class GetSplitBillQuery : IRequest<SplitBillDto?>
{
    public Guid OrderId { get; set; }
    public int  Ways    { get; set; }
}
