using MediatR;
using Tannous.Pos.Application.DTOs.Printing;

namespace Tannous.Pos.Application.Admin.Commands.ReprintReceipt;

public class ReprintReceiptCommand : IRequest<ReprintReceiptResult>
{
    public Guid OrderId { get; set; }
}

public class ReprintReceiptResult
{
    public bool Found { get; set; }
    public bool HasReceiptNumber { get; set; }
    public RenderResultDto? Receipt { get; set; }
}
