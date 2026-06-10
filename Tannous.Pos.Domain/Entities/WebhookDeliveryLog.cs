using Tannous.Pos.Domain.Common;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Domain.Entities;

/// <summary>
/// Audit record of one outbound webhook delivery attempt.
/// Kept for 30 days for debugging; older logs can be pruned.
/// </summary>
public class WebhookDeliveryLog : BaseEntity, IAggregateRoot
{
    public Guid             SubscriptionId { get; set; }
    public WebhookEventType EventType      { get; set; }
    public string           EventId        { get; set; } = string.Empty;
    public string           Payload        { get; set; } = string.Empty;
    public int?             ResponseCode   { get; set; }
    public bool             IsSuccess      { get; set; }
    public string?          ErrorMessage   { get; set; }
    public int              AttemptNumber  { get; set; } = 1;
    public long             DurationMs     { get; set; }

    public virtual WebhookSubscription Subscription { get; set; } = null!;
}
