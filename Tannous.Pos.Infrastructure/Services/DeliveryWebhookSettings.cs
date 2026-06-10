namespace Tannous.Pos.Infrastructure.Services;

/// <summary>
/// Per-channel webhook secrets used to validate inbound delivery platform requests (HMAC-SHA256).
/// Bound from configuration section "DeliveryWebhooks". Blank secrets enable dev/sandbox mode
/// (requests without a signature header are accepted by the channel adapters).
/// </summary>
public class DeliveryWebhookSettings
{
    public const string Section = "DeliveryWebhooks";

    public string TotersWebhookSecret  { get; set; } = string.Empty;
    public string TalabatWebhookSecret { get; set; } = string.Empty;
    public string WoltWebhookSecret    { get; set; } = string.Empty;
}
