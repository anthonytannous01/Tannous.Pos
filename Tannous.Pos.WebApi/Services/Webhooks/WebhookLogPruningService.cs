using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.WebApi.Services.Webhooks;

/// <summary>
/// Runs daily at 03:00 UTC. Deletes webhook delivery logs older than 30 days.
/// </summary>
public sealed class WebhookLogPruningService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WebhookLogPruningService> _logger;

    public WebhookLogPruningService(
        IServiceScopeFactory scopeFactory,
        ILogger<WebhookLogPruningService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var next3am = now.Date.AddHours(3);
            if (now.Hour >= 3) next3am = next3am.AddDays(1);

            try
            {
                await Task.Delay(next3am - now, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DbContext>();
                var cutoff = DateTime.UtcNow.AddDays(-30);

                var oldLogs = await db.Set<WebhookDeliveryLog>()
                    .Where(l => l.CreatedAt < cutoff)
                    .ToListAsync(stoppingToken);

                if (oldLogs.Count > 0)
                {
                    db.Set<WebhookDeliveryLog>().RemoveRange(oldLogs);
                    await db.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Pruned {Count} webhook delivery logs older than 30 days", oldLogs.Count);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Webhook log pruning failed");
            }
        }
    }
}
