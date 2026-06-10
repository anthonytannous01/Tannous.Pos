namespace Tannous.Pos.Domain.Enums;

public enum WebhookEventType
{
    OrderFinalized        = 10,
    OrderVoided           = 11,
    ReservationCreated    = 20,
    ReservationUpdated    = 21,
    LoyaltyPointsEarned   = 30,
    LoyaltyPointsRedeemed = 31,
    DeliveryOrderReceived = 40,
    DeliveryStatusChanged = 41,
    InventoryLowStock     = 50
}
