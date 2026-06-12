using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.DTOs.Receipts;
using Tannous.Pos.Application.Receipts.Queries.GetReceipt;

namespace Tannous.Pos.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize]
public class ReceiptsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReceiptsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{orderId:guid}")]
    public async Task<ActionResult<ReceiptDto>> GetReceipt(Guid orderId)
    {
        var result = await _mediator.Send(new GetReceiptQuery { OrderId = orderId });
        return result is null ? NotFound() : Ok(result);
    }
}
