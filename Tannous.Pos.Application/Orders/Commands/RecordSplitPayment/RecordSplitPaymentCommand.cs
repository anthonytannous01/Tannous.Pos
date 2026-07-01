using MediatR;
using Tannous.Pos.Application.DTOs.Orders;

namespace Tannous.Pos.Application.Orders.Commands.RecordSplitPayment;

public class RecordSplitPaymentCommand : IRequest<SplitBillDto>
{
    public Guid     OrderId   { get; set; }
    public int      TotalWays { get; set; }
    public decimal  Amount    { get; set; }
    public string   Method    { get; set; } = string.Empty;
    public string?  Reference { get; set; }
}
