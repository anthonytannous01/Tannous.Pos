using System.Text;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Tannous.Pos.Application.Delivery.Channels;
using Tannous.Pos.Application.Delivery.Commands.IngestChannelOrder;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Infrastructure.Services;

namespace Tannous.Pos.WebApi.Controllers;

/// <summary>
/// Inbound webhook receiver for external delivery platforms (Toters, Talabat, Wolt).
/// Authenticated by per-channel HMAC signature (not JWT), so the controller is [AllowAnonymous].
/// Approved in AllowAnonymousGovernanceTests allowlist.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/delivery/channels")]
[ApiVersion("1.0")]
[AllowAnonymous]
public class DeliveryWebhookController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IEnumerable<IDeliveryChannelAdapter> _adapters;
    private readonly DeliveryWebhookSettings _settings;
    private readonly ILogger<DeliveryWebhookController> _logger;

    public DeliveryWebhookController(
        IMediator mediator,
        IEnumerable<IDeliveryChannelAdapter> adapters,
        IOptions<DeliveryWebhookSettings> settings,
        ILogger<DeliveryWebhookController> logger)
    {
        _mediator = mediator;
        _adapters = adapters;
        _settings = settings.Value;
        _logger   = logger;
    }

    /// <summary>
    /// Ingest an order from a delivery platform: POST /delivery/channels/{channel}/orders.
    /// Returns 201 on creation, 200 on duplicate (idempotent), 401 on bad signature, 400 on bad payload.
    /// </summary>
    [HttpPost("{channel}/orders")]
    public async Task<IActionResult> IngestOrder(string channel, CancellationToken ct)
    {
        if (!TryResolveChannel(channel, out var deliveryChannel))
            return NotFound(new { error = $"Unknown delivery channel '{channel}'." });

        var adapter = _adapters.FirstOrDefault(a => a.Channel == deliveryChannel);
        if (adapter == null)
            return NotFound(new { error = $"No adapter registered for channel '{channel}'." });

        var rawBody = await ReadRawBodyAsync();

        var headers = Request.Headers.ToDictionary(
            h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase);

        var secret = ResolveSecret(deliveryChannel);

        if (!adapter.ValidateSignature(rawBody, headers, secret))
        {
            _logger.LogWarning("Rejected {Channel} webhook: invalid signature.", deliveryChannel);
            return Unauthorized(new { error = "Invalid webhook signature." });
        }

        var payload = adapter.ParseOrder(rawBody);
        if (payload == null)
        {
            _logger.LogWarning("Rejected {Channel} webhook: payload could not be parsed.", deliveryChannel);
            return BadRequest(new { error = "Payload could not be parsed." });
        }

        var result = await _mediator.Send(new IngestChannelOrderCommand
        {
            Channel = deliveryChannel,
            Payload = payload
        }, ct);

        if (result.IsDuplicate)
        {
            return Ok(new
            {
                orderId     = result.OrderId,
                orderNumber = result.OrderNumber,
                duplicate   = true
            });
        }

        return StatusCode(StatusCodes.Status201Created, new
        {
            orderId     = result.OrderId,
            orderNumber = result.OrderNumber
        });
    }

    private async Task<string> ReadRawBodyAsync()
    {
        Request.EnableBuffering();
        if (Request.Body.CanSeek) Request.Body.Position = 0;
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        if (Request.Body.CanSeek) Request.Body.Position = 0;
        return body;
    }

    private string ResolveSecret(DeliveryChannel channel) => channel switch
    {
        DeliveryChannel.Toters  => _settings.TotersWebhookSecret,
        DeliveryChannel.Talabat => _settings.TalabatWebhookSecret,
        DeliveryChannel.Wolt    => _settings.WoltWebhookSecret,
        _ => string.Empty
    };

    private static bool TryResolveChannel(string channel, out DeliveryChannel deliveryChannel)
    {
        switch (channel?.Trim().ToLowerInvariant())
        {
            case "toters":  deliveryChannel = DeliveryChannel.Toters;  return true;
            case "talabat": deliveryChannel = DeliveryChannel.Talabat; return true;
            case "wolt":    deliveryChannel = DeliveryChannel.Wolt;    return true;
            default:        deliveryChannel = default;                 return false;
        }
    }
}
