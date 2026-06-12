using MediatR;
using Tannous.Pos.Application.DTOs.Receipts;

namespace Tannous.Pos.Application.Receipts.Queries.GetReceipt;

public class GetReceiptQuery : IRequest<ReceiptDto?>
{
    public Guid OrderId { get; set; }
}
