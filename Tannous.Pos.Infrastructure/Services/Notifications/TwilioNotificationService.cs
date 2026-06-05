using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Infrastructure.Services.Notifications;

/// <summary>
/// Sends SMS or WhatsApp messages via Twilio REST API.
/// Uses IHttpClientFactory — never creates a raw HttpClient.
/// Never throws — returns false and logs on failure.
/// </summary>
public sealed class TwilioNotificationService : INotificationService
{
    private readonly IHttpClientFactory   _httpFactory;
    private readonly NotificationSettings _settings;
    private readonly ILogger<TwilioNotificationService> _logger;

    public TwilioNotificationService(
        IHttpClientFactory                    httpFactory,
        IOptions<NotificationSettings>        settings,
        ILogger<TwilioNotificationService>    logger)
    {
        _httpFactory = httpFactory;
        _settings    = settings.Value;
        _logger      = logger;
    }

    public async Task<bool> SendOrderConfirmationAsync(
        string toPhone, string orderNumber, string? receiptNumber,
        decimal totalAmount, string currency, string businessName,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled) return false;

        var twilio = _settings.Twilio;
        if (string.IsNullOrWhiteSpace(twilio.AccountSid) ||
            string.IsNullOrWhiteSpace(twilio.AuthToken)  ||
            string.IsNullOrWhiteSpace(twilio.FromNumber))
        {
            _logger.LogWarning("Twilio credentials incomplete — notification skipped for order {OrderNumber}", orderNumber);
            return false;
        }

        try
        {
            var isWhatsApp  = _settings.Provider.Equals("WhatsApp", StringComparison.OrdinalIgnoreCase);
            var fromAddress = isWhatsApp ? $"whatsapp:{twilio.FromNumber}" : twilio.FromNumber;
            var toAddress   = isWhatsApp ? $"whatsapp:{NormalizePhone(toPhone)}" : NormalizePhone(toPhone);

            var body = BuildMessage(orderNumber, receiptNumber, totalAmount, currency, businessName);

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("From", fromAddress),
                new KeyValuePair<string, string>("To",   toAddress),
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
                    "Order confirmation {Provider} sent to {Phone} for order {OrderNumber}",
                    _settings.Provider, MaskPhone(toPhone), orderNumber);
                return true;
            }

            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Twilio returned {StatusCode} for order {OrderNumber}: {Error}",
                (int)response.StatusCode, orderNumber, errorBody);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send order confirmation notification for order {OrderNumber}", orderNumber);
            return false;
        }
    }

    private static string BuildMessage(
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
