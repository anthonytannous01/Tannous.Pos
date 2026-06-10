using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Domain.Interfaces;

/// <summary>
/// Dispatches a POS event to all active subscribed endpoints.
/// Fire-and-forget — never throws, never blocks the calling transaction.
/// </summary>
public interface IWebhookDispatcher
{
    /// <summary>
    /// Dispatch an event. The payload object is serialized to JSON.
    /// Call after SaveChangesAsync — do NOT await (fire-and-forget).
    /// </summary>
    Task DispatchAsync(
        WebhookEventType eventType,
        object           payload,
        Guid?            branchId          = null,
        Guid?            subscriptionId    = null,
        CancellationToken cancellationToken = default);
}
