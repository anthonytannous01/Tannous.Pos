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
}
