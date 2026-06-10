using Tannous.Pos.Domain.Common;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Domain.Entities;

/// <summary>
/// A third-party endpoint subscribed to one or more POS events.
/// The secret is used to sign outbound payloads (HMAC-SHA256 in X-Tannous-Signature header).
/// </summary>
public class WebhookSubscription : BaseEntity, IAggregateRoot
{
    public string  Name             { get; set; } = string.Empty;
    public string  EndpointUrl      { get; set; } = string.Empty;
    public string  Secret           { get; set; } = string.Empty;
    public bool    IsActive         { get; set; } = true;
    public Guid?   BranchId         { get; set; }
    /// <summary>Comma-separated list of WebhookEventType int values (e.g. "10,11,20").</summary>
    public string  SubscribedEvents { get; set; } = string.Empty;

    public virtual Branch? Branch { get; set; }
    public virtual ICollection<WebhookDeliveryLog> DeliveryLogs { get; set; } = new List<WebhookDeliveryLog>();

    public IEnumerable<WebhookEventType> GetSubscribedEvents() =>
        string.IsNullOrWhiteSpace(SubscribedEvents)
            ? Enumerable.Empty<WebhookEventType>()
            : SubscribedEvents.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => (WebhookEventType)int.Parse(s.Trim()));

    public void SetSubscribedEvents(IEnumerable<WebhookEventType> events) =>
        SubscribedEvents = string.Join(',', events.Select(e => (int)e));
}
