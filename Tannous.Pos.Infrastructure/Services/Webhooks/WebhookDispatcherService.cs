using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Infrastructure.Services.Webhooks;

public sealed class WebhookDispatcherService : IWebhookDispatcher
{
    private readonly IHttpClientFactory    _httpFactory;
    private readonly IServiceScopeFactory  _scopeFactory;
    private readonly ILogger<WebhookDispatcherService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public WebhookDispatcherService(
        IHttpClientFactory httpFactory,
        IServiceScopeFactory scopeFactory,
        ILogger<WebhookDispatcherService> logger)
    {
        _httpFactory   = httpFactory;
        _scopeFactory  = scopeFactory;
        _logger        = logger;
    }

    public async Task DispatchAsync(
        WebhookEventType eventType,
        object payload,
        Guid? branchId = null,
        Guid? subscriptionId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            List<WebhookSubscription> subscriptions;
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<DbContext>();

                var query = db.Set<WebhookSubscription>()
                    .AsNoTracking()
                    .Where(s => s.IsActive);

                if (subscriptionId.HasValue)
                    query = query.Where(s => s.Id == subscriptionId.Value);
                else
                    query = query.Where(s => s.BranchId == null || s.BranchId == branchId);

                subscriptions = await query.ToListAsync(cancellationToken);
            }

            if (!subscriptionId.HasValue)
            {
                subscriptions = subscriptions
                    .Where(s => s.GetSubscribedEvents().Contains(eventType))
                    .ToList();
            }

            if (subscriptions.Count == 0)
                return;

            var eventId = Guid.NewGuid().ToString();
            var envelope = new
            {
                id        = eventId,
                @event    = eventType.ToString(),
                eventCode = (int)eventType,
                timestamp = DateTime.UtcNow.ToString("O"),
                branchId  = branchId?.ToString(),
                data      = payload
            };

            var json = JsonSerializer.Serialize(envelope, JsonOptions);

            foreach (var subscription in subscriptions)
                _ = DeliverAsync(subscription, eventType, eventId, json, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Webhook dispatch setup failed for {EventType}", eventType);
        }
    }

    private async Task DeliverAsync(
        WebhookSubscription subscription,
        WebhookEventType eventType,
        string eventId,
        string json,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        int? responseCode = null;
        var isSuccess = false;
        string? errorMessage = null;

        try
        {
            using var client = _httpFactory.CreateClient("Webhooks");
            client.Timeout = TimeSpan.FromSeconds(10);

            var signature = ComputeSignature(json, subscription.Secret);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, subscription.EndpointUrl)
            {
                Content = content
            };
            request.Headers.TryAddWithoutValidation("X-Tannous-Signature", $"sha256={signature}");

            using var response = await client.SendAsync(request, ct);
            responseCode = (int)response.StatusCode;
            isSuccess = response.IsSuccessStatusCode;

            if (!isSuccess)
                errorMessage = await response.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            _logger.LogWarning(ex,
                "Webhook delivery failed. SubscriptionId={SubscriptionId}, EventType={EventType}",
                subscription.Id, eventType);
        }
        finally
        {
            sw.Stop();

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DbContext>();

                db.Set<WebhookDeliveryLog>().Add(new WebhookDeliveryLog
                {
                    SubscriptionId = subscription.Id,
                    EventType      = eventType,
                    EventId        = eventId,
                    Payload        = json,
                    ResponseCode   = responseCode,
                    IsSuccess      = isSuccess,
                    ErrorMessage   = errorMessage,
                    AttemptNumber  = 1,
                    DurationMs     = sw.ElapsedMilliseconds
                });

                await db.SaveChangesAsync(ct);
            }
            catch (Exception logEx)
            {
                _logger.LogWarning(logEx,
                    "Failed to persist webhook delivery log. SubscriptionId={SubscriptionId}",
                    subscription.Id);
            }
        }
    }

    private static string ComputeSignature(string payload, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        using var hmac = new HMACSHA256(keyBytes);
        return Convert.ToBase64String(hmac.ComputeHash(payloadBytes));
    }
}
