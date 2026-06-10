using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Infrastructure.Services.Accounting;

public sealed class QuickBooksAccountingSync : IAccountingSync
{
    private const string TokenUrl = "https://oauth.platform.intuit.com/oauth2/v1/tokens/bearer";

    private readonly IHttpClientFactory _httpFactory;
    private readonly AccountingSettings _settings;
    private readonly DbContext          _dbContext;
    private readonly ILogger<QuickBooksAccountingSync> _logger;

    public QuickBooksAccountingSync(
        IHttpClientFactory httpFactory,
        IOptions<AccountingSettings> settings,
        DbContext dbContext,
        ILogger<QuickBooksAccountingSync> logger)
    {
        _httpFactory = httpFactory;
        _settings    = settings.Value;
        _dbContext   = dbContext;
        _logger      = logger;
    }

    public AccountingProvider Provider => AccountingProvider.QuickBooks;

    public async Task<bool> ExchangeCodeAsync(string code, string? branchId, CancellationToken ct = default)
    {
        try
        {
            var qb = _settings.QuickBooks;
            if (string.IsNullOrWhiteSpace(qb.ClientId) || string.IsNullOrWhiteSpace(qb.ClientSecret))
            {
                _logger.LogWarning("QuickBooks client credentials not configured");
                return false;
            }

            Guid? branchGuid = null;
            if (!string.IsNullOrWhiteSpace(branchId) && Guid.TryParse(branchId, out var parsed))
                branchGuid = parsed;

            var redirectUri = $"{_settings.BaseUrl.TrimEnd('/')}/api/v1/accounting/quickbooks/callback";
            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type",   "authorization_code"),
                new KeyValuePair<string, string>("code",         code),
                new KeyValuePair<string, string>("redirect_uri", redirectUri)
            });

            var client = _httpFactory.CreateClient("QuickBooks");
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{qb.ClientId}:{qb.ClientSecret}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.PostAsync(TokenUrl, form, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("QuickBooks token exchange failed: {Status} {Body}",
                    (int)response.StatusCode, body);
                return false;
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var accessToken  = root.GetProperty("access_token").GetString() ?? string.Empty;
            var refreshToken = root.GetProperty("refresh_token").GetString() ?? string.Empty;
            var expiresIn    = root.TryGetProperty("expires_in", out var expEl) ? expEl.GetInt32() : 3600;

            var connection = await _dbContext.Set<AccountingConnection>()
                .FirstOrDefaultAsync(c =>
                    c.Provider == AccountingProvider.QuickBooks && c.BranchId == branchGuid, ct);

            if (connection == null)
            {
                connection = new AccountingConnection
                {
                    Provider = AccountingProvider.QuickBooks,
                    BranchId = branchGuid,
                    IsActive = true
                };
                _dbContext.Set<AccountingConnection>().Add(connection);
            }
            else
            {
                connection.IsActive = true;
            }

            connection.AccessToken           = accessToken;
            connection.RefreshToken          = refreshToken;
            connection.AccessTokenExpiresAt  = DateTime.UtcNow.AddSeconds(expiresIn);
            connection.UpdatedAt             = DateTime.UtcNow;
            connection.LastSyncError         = null;

            await _dbContext.SaveChangesAsync(ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "QuickBooks ExchangeCodeAsync failed");
            return false;
        }
    }

    public async Task<bool> RefreshTokenAsync(AccountingConnection connection, CancellationToken ct = default)
    {
        try
        {
            var qb = _settings.QuickBooks;
            if (string.IsNullOrWhiteSpace(qb.ClientId) || string.IsNullOrWhiteSpace(qb.ClientSecret))
                return false;

            if (string.IsNullOrWhiteSpace(connection.RefreshToken))
                return false;

            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type",    "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", connection.RefreshToken)
            });

            var client = _httpFactory.CreateClient("QuickBooks");
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{qb.ClientId}:{qb.ClientSecret}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.PostAsync(TokenUrl, form, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("QuickBooks token refresh failed: {Status} {Body}",
                    (int)response.StatusCode, body);
                return false;
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            connection.AccessToken          = root.GetProperty("access_token").GetString() ?? connection.AccessToken;
            if (root.TryGetProperty("refresh_token", out var rt))
                connection.RefreshToken = rt.GetString() ?? connection.RefreshToken;
            var expiresIn = root.TryGetProperty("expires_in", out var expEl) ? expEl.GetInt32() : 3600;
            connection.AccessTokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn);
            connection.UpdatedAt            = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "QuickBooks RefreshTokenAsync failed");
            return false;
        }
    }

    public async Task<(bool Success, string? ExternalRef, string? Error)> SyncDayAsync(
        AccountingConnection connection, DateTime date, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(connection.CompanyId))
                return (false, null, "QuickBooks company ID (realmId) is not configured");

            var dayStart = date.Date;
            var dayEnd   = dayStart.AddDays(1);

            var ordersQuery = _dbContext.Set<Order>()
                .Include(o => o.Payments)
                .Where(o => o.Status == OrderStatus.Paid
                    && o.CreatedAt >= dayStart
                    && o.CreatedAt < dayEnd);

            if (connection.BranchId.HasValue)
                ordersQuery = ordersQuery.Where(o => o.BranchId == connection.BranchId);

            var orders = await ordersQuery.ToListAsync(ct);

            if (orders.Count == 0)
                return (true, null, null);

            if (connection.AccessTokenExpiresAt < DateTime.UtcNow.AddMinutes(5))
            {
                var refreshed = await RefreshTokenAsync(connection, ct);
                if (!refreshed)
                    return (false, null, "Failed to refresh QuickBooks access token");
            }

            var netSales  = orders.Sum(o => o.TotalAmount);
            var taxAmount = orders.Sum(o => o.TaxAmount);
            var revenue   = netSales - taxAmount;

            var payments = orders.SelectMany(o => o.Payments).ToList();
            decimal PaymentUsd(Payment p) => p.AmountInUsd > 0 ? p.AmountInUsd : p.Amount;

            var cashTotal = payments
                .Where(p => p.PaymentMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase))
                .Sum(PaymentUsd);
            var cardTotal = payments
                .Where(p => !p.PaymentMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase))
                .Sum(PaymentUsd);

            // TODO: make configurable in a future step — hard-coded GL account names for now.
            var lines = new List<object>();

            if (cashTotal > 0)
            {
                lines.Add(CreateJournalLine(cashTotal, "Debit", "Cash", $"Daily sales — Cash ({dayStart:yyyy-MM-dd})"));
            }

            if (cardTotal > 0)
            {
                lines.Add(CreateJournalLine(cardTotal, "Debit", "Card Receivable",
                    $"Daily sales — Card ({dayStart:yyyy-MM-dd})"));
            }

            if (revenue > 0)
            {
                lines.Add(CreateJournalLine(revenue, "Credit", "Sales Revenue",
                    $"Daily sales revenue ({dayStart:yyyy-MM-dd})"));
            }

            if (taxAmount > 0)
            {
                lines.Add(CreateJournalLine(taxAmount, "Credit", "Sales Tax Payable",
                    $"Sales tax collected ({dayStart:yyyy-MM-dd})"));
            }

            if (lines.Count == 0)
                return (true, null, null);

            var payload = new
            {
                Line        = lines,
                PrivateNote = $"Tannous POS daily sync {dayStart:yyyy-MM-dd}"
            };

            var host = _settings.QuickBooks.Sandbox.Equals("true", StringComparison.OrdinalIgnoreCase)
                ? "sandbox-quickbooks.api.intuit.com"
                : "quickbooks.api.intuit.com";

            var url = $"https://{host}/v3/company/{connection.CompanyId}/journalentry?minorversion=65";
            var json = JsonSerializer.Serialize(payload);

            var client = _httpFactory.CreateClient("QuickBooks");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", connection.AccessToken);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.PostAsync(url,
                new StringContent(json, Encoding.UTF8, "application/json"), ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                var externalId = TryParseJournalEntryId(responseBody);
                return (true, externalId, null);
            }

            return (false, null, responseBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "QuickBooks SyncDayAsync failed for {Date}", date.ToString("yyyy-MM-dd"));
            return (false, null, ex.Message);
        }
    }

    private static object CreateJournalLine(decimal amount, string postingType, string accountName, string description)
        => new
        {
            DetailType = "JournalEntryLineDetail",
            Amount     = Math.Round(amount, 2),
            Description = description,
            JournalEntryLineDetail = new
            {
                PostingType = postingType,
                AccountRef  = new { name = accountName }
            }
        };

    private static string? TryParseJournalEntryId(string responseBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("JournalEntry", out var je)
                && je.TryGetProperty("Id", out var idEl))
                return idEl.GetString();
        }
        catch
        {
            // ignore parse errors
        }

        return null;
    }
}
