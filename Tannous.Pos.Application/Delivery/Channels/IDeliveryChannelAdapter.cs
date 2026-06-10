using Tannous.Pos.Application.DTOs.Delivery;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Delivery.Channels;

/// <summary>
/// Normalises an incoming webhook payload from an external delivery channel
/// into a <see cref="CreateChannelOrderDto"/>. Each channel has exactly one adapter implementation.
/// Implementations must be stateless and never throw — return null on parse failure.
///
/// NOTE: This abstraction lives in the Application layer (not Domain) because it returns an
/// Application DTO; placing it in Domain would invert the layer direction (Domain → Application).
/// Headers are passed as a framework-agnostic dictionary so the Application layer stays free of
/// ASP.NET coupling — the WebApi controller adapts <c>IHeaderDictionary</c> before calling.
/// </summary>
public interface IDeliveryChannelAdapter
{
    DeliveryChannel Channel { get; }

    /// <summary>
    /// Validate the request signature/HMAC for this channel.
    /// Returns true if the request is authentic, false to reject with 401.
    /// </summary>
    bool ValidateSignature(string rawBody, IReadOnlyDictionary<string, string> headers, string webhookSecret);

    /// <summary>
    /// Parse the raw JSON body into a normalised order.
    /// Returns null if the payload cannot be parsed (caller logs and returns 400).
    /// </summary>
    CreateChannelOrderDto? ParseOrder(string rawBody);
}
