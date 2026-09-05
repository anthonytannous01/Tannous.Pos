using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Tannous.Pos.Application.DTOs.Menu;
using Tannous.Pos.Application.Kiosk.Commands.CreateKioskOrder;
using Tannous.Pos.Application.Menu.Queries.GetPublicMenu;

namespace Tannous.Pos.WebApi.Controllers;

/// <summary>
/// Self-ordering kiosk endpoints — fully unauthenticated.
/// Customers browse the menu and place orders without staff intervention.
/// Orders are placed in Pending status; staff finalizes at the counter.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/kiosk")]
[ApiVersion("1.0")]
[AllowAnonymous]
[EnableRateLimiting("PublicRead")]
public class KioskController : ControllerBase
{
    private readonly IMediator _mediator;

    public KioskController(IMediator mediator) => _mediator = mediator;

    /// <summary>Public menu for the kiosk — categories, items, prices, Arabic names.</summary>
    [HttpGet("menu")]
    public async Task<ActionResult<PublicMenuDto>> GetMenu(CancellationToken ct)
        => Ok(await _mediator.Send(new GetPublicMenuQuery(), ct));

    /// <summary>
    /// Place a kiosk order — no auth required.
    /// Returns the order number for the customer to show at the counter.
    /// </summary>
    [HttpPost("orders")]
    [EnableRateLimiting("PublicWrite")]
    public async Task<ActionResult<KioskOrderResultDto>> PlaceOrder(
        [FromBody] CreateKioskOrderCommand command, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(command, ct);
            return CreatedAtAction(nameof(PlaceOrder), result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
