using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.WebApi.Services.Accounting;

/// <summary>
/// Runs daily at 02:00 UTC. Syncs the previous day's sales to all connected accounting providers.
/// Uses IServiceScopeFactory to create a scoped DbContext per run.
/// </summary>
public sealed class DailySalesSyncBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DailySalesSyncBackgroundService> _logger;

    public DailySalesSyncBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<DailySalesSyncBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var next2am = now.Date.AddHours(2);
            if (now.Hour >= 2) next2am = next2am.AddDays(1);
            var delay = next2am - now;

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var coordinator = scope.ServiceProvider.GetRequiredService<IAccountingSyncCoordinator>();
                await coordinator.RunSyncAsync(DateTime.UtcNow.Date.AddDays(-1), branchId: null, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Daily accounting sync failed");
            }
        }
    }
}
