namespace Tannous.Pos.Domain.Enums;

public enum OrderStatus
{
    Open = 1,
    Pending = 2,
    Confirmed = 3,
    InPreparation = 4,
    Ready = 5,
    Paid = 6,
    Completed = 7,
    Cancelled = 8,
    Void = 9
}
