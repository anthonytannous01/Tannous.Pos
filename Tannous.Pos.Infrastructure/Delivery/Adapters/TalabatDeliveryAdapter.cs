using System.Text.Json;
using Tannous.Pos.Application.Delivery.Channels;
using Tannous.Pos.Application.DTOs.Delivery;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Infrastructure.Delivery.Adapters;

/// <summary>
/// Adapter for Talabat delivery webhooks.
/// Signature header: X-Talabat-Hmac-Sha256 (HMAC-SHA256 of the raw body).
/// </summary>
public sealed class TalabatDeliveryAdapter : IDeliveryChannelAdapter
{
    public const string SignatureHeader = "X-Talabat-Hmac-Sha256";

    public DeliveryChannel Channel => DeliveryChannel.Talabat;

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
                ExternalOrderId  = root.GetStringOrEmpty("orderId"),
                CustomerName     = root.GetStringOrEmpty("customerName"),
                CustomerPhone    = root.GetStringOrNull("customerPhone"),
                DeliveryAddress  = root.GetStringOrEmpty("deliveryAddress"),
                ApartmentDetails = root.GetStringOrNull("buildingDetails"),
                Notes            = root.GetStringOrNull("specialInstructions"),
                DeliveryFee      = root.GetDecimalOrZero("deliveryCharge"),
                EstimatedMinutes = root.GetIntOrNull("estimatedDeliveryTime")
            };

            if (root.TryGetProperty("orderItems", out var items) && items.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in items.EnumerateArray())
                {
                    dto.Lines.Add(new ChannelOrderLineDto
                    {
                        ExternalItemId = item.GetStringOrNull("itemCode"),
                        ItemName       = item.GetStringOrEmpty("itemName"),
                        Quantity       = item.GetIntOrNull("quantity") ?? 1,
                        UnitPrice      = item.GetDecimalOrZero("price"),
                        Notes          = item.GetStringOrNull("itemNotes")
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
