using Tannous.Pos.Domain.Common;
using Tannous.Pos.Domain.Common.ValueObjects;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Domain.Entities;

/// <summary>
/// Immutable record of a single loyalty point change on an account.
/// Never update rows — always append a new transaction.
/// </summary>
public class LoyaltyTransaction : BaseEntity
{
    public Guid LoyaltyAccountId { get; set; }

    /// <summary>Points added (positive) or redeemed (negative).</summary>
    public int Points { get; set; }

    public LoyaltyTransactionType TransactionType { get; set; }

    /// <summary>Order that triggered this transaction (nullable — manual adjustments have no order).</summary>
    public Guid? OrderId { get; set; }

    public string? Notes { get; set; }

    // Navigation
    public virtual LoyaltyAccount LoyaltyAccount { get; set; } = null!;
}
