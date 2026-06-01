using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Integration;

/// <summary>Isolated from hit/reuse tests so replay-storm cache state does not force reconciliation bypass on every case.</summary>
public class OperationalReconciliationCacheBypassIntegrationTests : IntegrationTestBase
{
    private const string ReconciliationBase = "/api/v1.0/internal/operational-audit/reconciliation";

    public OperationalReconciliationCacheBypassIntegrationTests(IntegrationPostgresFixture postgresFixture) : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Replay_storm_pressure_bypasses_reconciliation_summary_cache()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        await SeedReplayStormReceiptsInScopeAsync(OperationalResilienceConstants.ReplayStormReceiptCountThreshold + 1);

        var telemetry = GetCacheTelemetry();
        var category = OperationalDiagnosticsCacheCategories.ReconciliationSummary;
        var bypassBefore = GetCategoryBypasses(telemetry, category);

        (await _client.GetAsync("/api/v1.0/internal/operational-audit/resilience/summary")).EnsureSuccessStatusCode();
        (await _client.GetAsync($"{ReconciliationBase}/summary")).EnsureSuccessStatusCode();

        Assert.True(GetCategoryBypasses(telemetry, category) > bypassBefore);
    }

    private async Task SeedReplayStormReceiptsInScopeAsync(int count)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PosDbContext>();
        const string deviceId = "reconciliation-cache-replay-storm";
        for (var i = 0; i < count; i++)
        {
            db.SyncOperationReceipts.Add(new SyncOperationReceipt
            {
                Id = Guid.NewGuid(),
                DeviceId = deviceId,
                OperationId = $"recon-cache-replay-{i:D4}",
                OperationType = "AdjustInventory",
                ProcessedAtUtc = DateTime.UtcNow,
                Success = true
            });
        }

        await db.SaveChangesAsync();
    }

    private IOperationalDiagnosticsCacheTelemetry GetCacheTelemetry()
    {
        using var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IOperationalDiagnosticsCacheTelemetry>();
    }

    private static long GetCategoryBypasses(IOperationalDiagnosticsCacheTelemetry telemetry, string category) =>
        telemetry.GetSnapshot().ByCategory.TryGetValue(category, out var stats) ? stats.Bypasses : 0;
}
