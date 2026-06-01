using Tannous.Pos.Domain.Common;
using Tannous.Pos.Domain.Common.ValueObjects;

namespace Tannous.Pos.Domain.Entities;

/// <summary>
/// Internal consistency record for refunds (no external payment processor). Created on paid void.
/// </summary>
public class PaymentRefund : BaseEntity, IAggregateRoot
{
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    /// <summary>Client/idempotency or void correlation identifier (replay-safe dedupe).</summary>
    public string CorrelationId { get; set; } = string.Empty;
    public Guid? OriginalPaymentId { get; set; }

    public virtual Order Order { get; set; } = null!;
    public virtual Payment? OriginalPayment { get; set; }
}
