namespace Tannous.Pos.Domain.Enums;

/// <summary>
/// Tracks the kitchen preparation lifecycle of a single order line on the Kitchen Display System.
/// Transitions: Pending → InProgress → Done. Cancelled mirrors order void.
/// </summary>
public enum KdsStatus
{
    /// <summary>Order line received, not yet acknowledged by kitchen.</summary>
    Pending = 0,

    /// <summary>Kitchen has started preparing this line item.</summary>
    InProgress = 1,

    /// <summary>Item is ready / served.</summary>
    Done = 2,

    /// <summary>Line was cancelled (order voided or item removed).</summary>
    Cancelled = 3
}
