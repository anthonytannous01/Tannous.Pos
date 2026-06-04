using Tannous.Pos.Domain.Common;
using Tannous.Pos.Domain.Common.ValueObjects;

namespace Tannous.Pos.Domain.Entities;

/// <summary>
/// Loyalty account belonging to a customer. Separate aggregate — never coupled to Order core.
/// Points are earned on order finalization and redeemed at payment.
/// </summary>
public class LoyaltyAccount : BaseEntity, IAggregateRoot
{
    public Guid CustomerId { get; set; }

    /// <summary>Current redeemable point balance.</summary>
    public int PointBalance { get; set; } = 0;

    /// <summary>Lifetime points earned (never decremented on redemption — for analytics).</summary>
    public int LifetimePointsEarned { get; set; } = 0;

    /// <summary>Lifetime points redeemed.</summary>
    public int LifetimePointsRedeemed { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual Customer Customer { get; set; } = null!;
    public virtual ICollection<LoyaltyTransaction> Transactions { get; set; } = new List<LoyaltyTransaction>();
}
