using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.DTOs.Loyalty;
using Tannous.Pos.Application.Loyalty.Commands.EarnPoints;
using Tannous.Pos.Application.Loyalty.Commands.RedeemPoints;
using Tannous.Pos.Application.Loyalty.Queries.GetLoyaltyAccount;
using Tannous.Pos.WebApi.Constants;

namespace Tannous.Pos.WebApi.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/loyalty")]
[ApiVersion("1.0")]
[Authorize(Policy = PolicyConstants.CanSell)]
public class LoyaltyController : ControllerBase
{
    private readonly IMediator _mediator;

    public LoyaltyController(IMediator mediator) => _mediator = mediator;

    /// <summary>Get loyalty account for a customer. Returns 404 if no account exists yet.</summary>
    [HttpGet("customers/{customerId:guid}")]
    public async Task<ActionResult<LoyaltyAccountDto>> GetAccount(
        Guid customerId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetLoyaltyAccountQuery { CustomerId = customerId }, cancellationToken);
        return result == null ? NotFound() : Ok(result);
    }

    /// <summary>Manually credit points to a customer (e.g. for a complaint resolution).</summary>
    [HttpPost("customers/{customerId:guid}/earn")]
    [Authorize(Policy = PolicyConstants.CanManageCustomers)]
    public async Task<ActionResult<LoyaltyAccountDto>> Earn(
        Guid customerId, [FromBody] EarnPointsDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new EarnPointsCommand
            {
                CustomerId = customerId,
                Points     = dto.Points,
                OrderId    = dto.OrderId,
                Notes      = dto.Notes
            }, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Redeem points for a customer at checkout.</summary>
    [HttpPost("customers/{customerId:guid}/redeem")]
    public async Task<ActionResult<LoyaltyAccountDto>> Redeem(
        Guid customerId, [FromBody] RedeemPointsDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new RedeemPointsCommand
            {
                CustomerId = customerId,
                Points     = dto.Points,
                OrderId    = dto.OrderId
            }, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
