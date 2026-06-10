namespace Tannous.Pos.Domain.Interfaces;

/// <summary>
/// Sends customer-facing notifications (SMS or WhatsApp).
/// Implementations must be non-throwing — failures are logged and swallowed by callers.
/// The default registration is <see cref="NullNotificationService"/> (no-op).
/// Twilio is activated when Notifications:Twilio:AccountSid is configured.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Send an order confirmation message to the customer's phone.
    /// Returns true on success, false on failure (never throws).
    /// </summary>
    Task<bool> SendOrderConfirmationAsync(
        string   toPhone,
        string   orderNumber,
        string?  receiptNumber,
        decimal  totalAmount,
        string   currency,
        string   businessName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send an operator-authored loyalty/CRM message (e.g. a campaign) to a customer's phone.
    /// The message body is passed through verbatim. Returns true on success, false on failure (never throws).
    /// </summary>
    Task<bool> SendLoyaltyNotificationAsync(
        string   toPhone,
        string   message,
        string   businessName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Notify a customer that they earned loyalty points.
    /// Returns true on success, false on failure (never throws).
    /// </summary>
    Task<bool> SendPointsEarnedNotificationAsync(
        string   toPhone,
        int      pointsEarned,
        int      newBalance,
        string   businessName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a reservation confirmation to a customer.
    /// Returns true on success, false on failure (never throws).
    /// </summary>
    Task<bool> SendReservationConfirmationAsync(
        string   toPhone,
        string   customerName,
        DateTime reservationDateTime,
        int      partySize,
        string?  tableName,
        string   businessName,
        CancellationToken cancellationToken = default);
}
