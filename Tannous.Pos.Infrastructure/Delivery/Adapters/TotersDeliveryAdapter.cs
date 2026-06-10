using System.Text.Json;
using Tannous.Pos.Application.Delivery.Channels;
using Tannous.Pos.Application.DTOs.Delivery;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Infrastructure.Delivery.Adapters;

/// <summary>
/// Adapter for Toters (Lebanon) delivery webhooks.
/// Signature header: X-Toters-Signature (HMAC-SHA256 of the raw body).
/// </summary>
public sealed class TotersDeliveryAdapter : IDeliveryChannelAdapter
{
    public const string SignatureHeader = "X-Toters-Signature";

    public DeliveryChannel Channel => DeliveryChannel.Toters;

    public bool ValidateSignature(
        string rawBody, IReadOnlyDictionary<string, string> headers, string webhookSecret)
    {
        var hasHeader = headers.TryGetValue(SignatureHeader, out var provided)
                        && !string.IsNullOrWhiteSpace(provided);

        // Dev/sandbox mode: no secret configured and no signature header → accept.
        if (string.IsNullOrWhiteSpace(webhookSecret) && !hasHeader)
            return true;

        if (string.IsNullOrWhiteSpace(webhookSecret) || !hasHeader)
            return false;

        return DeliveryWebhookSignature.IsValid(rawBody, provided!, webhookSecret);
    }

    public CreateChannelOrderDto? ParseOrder(string rawBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;

            var dto = new CreateChannelOrderDto
            {
                ExternalOrderId = root.GetStringOrEmpty("order_id")
            };

            if (root.TryGetProperty("customer", out var customer) && customer.ValueKind == JsonValueKind.Object)
            {
                dto.CustomerName  = customer.GetStringOrEmpty("name");
                dto.CustomerPhone = customer.GetStringOrNull("phone");
            }

            if (root.TryGetProperty("delivery", out var delivery) && delivery.ValueKind == JsonValueKind.Object)
            {
                dto.DeliveryAddress  = delivery.GetStringOrEmpty("address");
                dto.ApartmentDetails = delivery.GetStringOrNull("apartment");
                dto.Notes            = delivery.GetStringOrNull("notes");
                dto.DeliveryFee      = delivery.GetDecimalOrZero("fee");
                dto.EstimatedMinutes = delivery.GetIntOrNull("estimated_minutes");
            }

            if (root.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in items.EnumerateArray())
                {
                    dto.Lines.Add(new ChannelOrderLineDto
                    {
                        ExternalItemId = item.GetStringOrNull("id"),
                        ItemName       = item.GetStringOrEmpty("name"),
                        Quantity       = item.GetIntOrNull("quantity") ?? 1,
                        UnitPrice      = item.GetDecimalOrZero("unit_price"),
                        Notes          = item.GetStringOrNull("notes")
                    });
                }
            }

            return dto;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
