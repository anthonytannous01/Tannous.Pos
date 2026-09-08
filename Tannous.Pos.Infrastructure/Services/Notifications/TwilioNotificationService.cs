using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Infrastructure.Services.Notifications;

/// <summary>
/// Sends customer notifications over WhatsApp via the Twilio REST API.
/// Uses IHttpClientFactory — never creates a raw HttpClient. Never throws: returns false and logs.
///
/// WhatsApp only. SMS was removed rather than kept as an option: a channel nobody has configured
/// still has to be reasoned about at every call site, and the four send paths here each carried
/// their own copy of the provider branch. One channel, one code path.
///
/// Every message this class sends is business-initiated, which matters in production. Outside a
/// 24-hour window opened by the customer messaging the business first, WhatsApp only delivers
/// pre-approved message templates. Twilio's sandbox hides this, because joining the sandbox opens
/// that window: a message that arrives fine in testing can be rejected in production until the
/// corresponding template is approved. See TODO.md.
/// </summary>
public sealed class TwilioNotificationService : INotificationService
{
    private readonly IHttpClientFactory   _httpFactory;
    private readonly NotificationSettings _settings;
    private readonly ILogger<TwilioNotificationService> _logger;

    public TwilioNotificationService(
        IHttpClientFactory                 httpFactory,
        IOptions<NotificationSettings>     settings,
        ILogger<TwilioNotificationService> logger)
    {
        _httpFactory = httpFactory;
        _settings    = settings.Value;
        _logger      = logger;
    }

    public Task<bool> SendOrderConfirmationAsync(
        string toPhone, string orderNumber, string? receiptNumber,
        decimal totalAmount, string currency, string businessName,
        CancellationToken cancellationToken = default)
        => SendAsync(
            toPhone,
            BuildOrderConfirmationMessage(orderNumber, receiptNumber, totalAmount, currency, businessName),
            "order confirmation",
            cancellationToken);

    public Task<bool> SendLoyaltyNotificationAsync(
        string toPhone, string message, string businessName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message)) return Task.FromResult(false);
        return SendAsync(toPhone, message, "loyalty notification", cancellationToken);
    }

    public Task<bool> SendPointsEarnedNotificationAsync(
        string toPhone, int pointsEarned, int newBalance, string businessName,
        CancellationToken cancellationToken = default)
        => SendAsync(
            toPhone,
            BuildPointsEarnedMessage(pointsEarned, newBalance, businessName),
            "points-earned notification",
            cancellationToken);

    public Task<bool> SendReservationConfirmationAsync(
        string toPhone, string customerName, DateTime reservationDateTime,
        int partySize, string? tableName, string businessName,
        CancellationToken cancellationToken = default)
        => SendAsync(
            toPhone,
            BuildReservationConfirmationMessage(customerName, reservationDateTime, partySize, tableName, businessName),
            "reservation confirmation",
            cancellationToken);

    /// <summary>
    /// The single path to Twilio. Every public method above differs only in the body it builds, so
    /// the credential check, address formatting, auth header, error handling and phone masking all
    /// live here once. They used to be copied four times, which is how one copy drifts.
    /// </summary>
    private async Task<bool> SendAsync(
        string toPhone, string body, string purpose, CancellationToken cancellationToken)
    {
        if (!_settings.Enabled) return false;
        if (string.IsNullOrWhiteSpace(toPhone)) return false;

        var twilio = _settings.Twilio;
        if (string.IsNullOrWhiteSpace(twilio.AccountSid) ||
            string.IsNullOrWhiteSpace(twilio.AuthToken)  ||
            string.IsNullOrWhiteSpace(twilio.FromNumber))
        {
            _logger.LogWarning(
                "Twilio credentials incomplete — {Purpose} skipped for {Phone}",
                purpose, MaskPhone(toPhone));
            return false;
        }

        try
        {
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("From", WhatsAppAddress(twilio.FromNumber)),
                new KeyValuePair<string, string>("To",   WhatsAppAddress(NormalizePhone(toPhone))),
                new KeyValuePair<string, string>("Body", body)
            });

            var client = _httpFactory.CreateClient("Twilio");
            var credentials = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{twilio.AccountSid}:{twilio.AuthToken}"));
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", credentials);

            var url = $"https://api.twilio.com/2010-04-01/Accounts/{twilio.AccountSid}/Messages.json";
            var response = await client.PostAsync(url, formContent, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "WhatsApp {Purpose} sent to {Phone}", purpose, MaskPhone(toPhone));
                return true;
            }

            // Twilio's body carries the actionable reason: 63016 is "no approved template outside
            // the 24-hour window", 63007 a bad sender, 21211 a malformed number.
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Twilio returned {StatusCode} for {Purpose} to {Phone}: {Error}",
                (int)response.StatusCode, purpose, MaskPhone(toPhone), errorBody);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send {Purpose} to {Phone}", purpose, MaskPhone(toPhone));
            return false;
        }
    }

    /// <summary>Twilio addresses WhatsApp endpoints as "whatsapp:+E164"; tolerate a prefixed setting.</summary>
    private static string WhatsAppAddress(string phone) =>
        phone.StartsWith("whatsapp:", StringComparison.OrdinalIgnoreCase) ? phone : $"whatsapp:{phone}";

    private static string BuildOrderConfirmationMessage(
        string orderNumber, string? receiptNumber,
        decimal totalAmount, string currency, string businessName)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"✅ Order confirmed at {businessName}");
        sb.AppendLine($"Order: #{orderNumber}");
        if (!string.IsNullOrEmpty(receiptNumber))
            sb.AppendLine($"Receipt: #{receiptNumber}");
        sb.AppendLine($"Total: {currency} {totalAmount:N2}");
        sb.Append("Thank you!");
        return sb.ToString();
    }

    private static string BuildPointsEarnedMessage(int pointsEarned, int newBalance, string businessName)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"🎉 You earned {pointsEarned} loyalty points at {businessName}!");
        sb.AppendLine($"Your new balance: {newBalance} points.");
        sb.Append("Thank you for your visit!");
        return sb.ToString();
    }

    private static string BuildReservationConfirmationMessage(
        string customerName, DateTime reservationDateTime,
        int partySize, string? tableName, string businessName)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"✅ Reservation confirmed at {businessName}!");
        sb.AppendLine($"Name: {customerName}");
        sb.AppendLine($"Date: {reservationDateTime:dddd, MMMM d 'at' h:mm tt}");
        sb.AppendLine($"Party size: {partySize}");
        if (!string.IsNullOrWhiteSpace(tableName))
            sb.AppendLine($"Table: {tableName}");
        sb.Append("See you soon!");
        return sb.ToString();
    }

    /// <summary>Normalise Lebanese numbers: ensure E.164 format.</summary>
    private static string NormalizePhone(string phone)
    {
        phone = phone.Trim().Replace(" ", "").Replace("-", "");
        if (!phone.StartsWith('+'))
            phone = "+961" + phone.TrimStart('0');
        return phone;
    }

    private static string MaskPhone(string phone)
    {
        if (phone.Length <= 4) return "****";
        return phone[..^4] + "****";
    }
}
