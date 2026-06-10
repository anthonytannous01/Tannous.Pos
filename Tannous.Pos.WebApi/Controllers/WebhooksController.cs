using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.DTOs.Integrations;
using Tannous.Pos.Application.Integrations.Webhooks.Commands.CreateWebhookSubscription;
using Tannous.Pos.Application.Integrations.Webhooks.Commands.DeleteWebhookSubscription;
using Tannous.Pos.Application.Integrations.Webhooks.Commands.TestWebhookSubscription;
using Tannous.Pos.Application.Integrations.Webhooks.Commands.UpdateWebhookSubscription;
using Tannous.Pos.Application.Integrations.Webhooks.Queries.GetWebhookDeliveryLogs;
using Tannous.Pos.Application.Integrations.Webhooks.Queries.GetWebhookSubscriptions;
using Tannous.Pos.WebApi.Constants;

namespace Tannous.Pos.WebApi.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/webhooks")]
[ApiVersion("1.0")]
[Authorize(Policy = PolicyConstants.CanManageSettings)]
public class WebhooksController : ControllerBase
{
    private readonly IMediator _mediator;

    public WebhooksController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<List<WebhookSubscriptionDto>>> GetSubscriptions(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetWebhookSubscriptionsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<CreateWebhookResponse>> CreateSubscription(
        [FromBody] CreateWebhookSubscriptionDto subscription,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateWebhookSubscriptionCommand
        {
            Subscription = subscription
        }, cancellationToken);

        return CreatedAtAction(nameof(GetSubscriptions), result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<WebhookSubscriptionDto>> UpdateSubscription(
        Guid id,
        [FromBody] UpdateWebhookSubscriptionDto subscription,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdateWebhookSubscriptionCommand
        {
            Id           = id,
            Subscription = subscription
        }, cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteSubscription(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteWebhookSubscriptionCommand { Id = id }, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/logs")]
    public async Task<ActionResult<List<WebhookDeliveryLogDto>>> GetDeliveryLogs(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetWebhookDeliveryLogsQuery
        {
            SubscriptionId = id
        }, cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id:guid}/test")]
    public async Task<IActionResult> TestSubscription(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new TestWebhookSubscriptionCommand { Id = id }, cancellationToken);
        return Ok(new { success = true });
    }
}
