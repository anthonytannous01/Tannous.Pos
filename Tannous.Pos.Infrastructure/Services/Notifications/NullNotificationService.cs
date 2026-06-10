using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Infrastructure.Services.Notifications;

/// <summary>
/// No-op implementation. Registered when notifications are disabled or Twilio credentials are absent.
/// </summary>
public sealed class NullNotificationService : INotificationService
{
    public Task<bool> SendOrderConfirmationAsync(
        string toPhone, string orderNumber, string? receiptNumber,
        decimal totalAmount, string currency, string businessName,
        CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task<bool> SendLoyaltyNotificationAsync(
        string toPhone, string message, string businessName,
        CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task<bool> SendPointsEarnedNotificationAsync(
        string toPhone, int pointsEarned, int newBalance, string businessName,
        CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task<bool> SendReservationConfirmationAsync(
        string toPhone, string customerName, DateTime reservationDateTime,
        int partySize, string? tableName, string businessName,
        CancellationToken cancellationToken = default)
        => Task.FromResult(false);
}
