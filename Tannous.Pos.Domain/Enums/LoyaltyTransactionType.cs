namespace Tannous.Pos.Domain.Enums;

public enum LoyaltyTransactionType
{
    Earn      = 0,  // Points credited on order finalization
    Redeem    = 1,  // Points spent at payment
    Adjust    = 2,  // Manual adjustment by owner/manager
    Expire    = 3   // Future: point expiry
}
