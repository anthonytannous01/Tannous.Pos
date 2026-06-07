namespace Tannous.Pos.Domain.Enums;

public enum DeliveryStatus
{
    Pending   = 0,  // order placed, driver not yet assigned
    Assigned  = 1,  // driver assigned, not yet picked up
    PickedUp  = 2,  // driver has the order
    OnWay     = 3,  // en route to customer
    Delivered = 4,  // successfully delivered
    Failed    = 5,  // delivery failed (customer not home, wrong address, etc.)
    Cancelled = 6
}
