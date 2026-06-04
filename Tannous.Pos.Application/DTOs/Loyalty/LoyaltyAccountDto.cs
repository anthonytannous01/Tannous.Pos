using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.DTOs.Loyalty;

public class LoyaltyAccountDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int PointBalance { get; set; }
    public int LifetimePointsEarned { get; set; }
    public int LifetimePointsRedeemed { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<LoyaltyTransactionDto> RecentTransactions { get; set; } = new();
}

public class LoyaltyTransactionDto
{
    public Guid Id { get; set; }
    public int Points { get; set; }
    public LoyaltyTransactionType TransactionType { get; set; }
    public Guid? OrderId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class EarnPointsDto
{
    /// <summary>Points to credit. Must be positive.</summary>
    public int Points { get; set; }
    public Guid? OrderId { get; set; }
    public string? Notes { get; set; }
}

public class RedeemPointsDto
{
    /// <summary>Points to redeem. Must be positive and ≤ current balance.</summary>
    public int Points { get; set; }
    public Guid? OrderId { get; set; }
}

public class AdjustPointsDto
{
    /// <summary>Positive to add, negative to deduct.</summary>
    public int Points { get; set; }
    public string? Notes { get; set; }
}
