using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Sync;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Integration;

public class OperationalCacheInvalidationIntegrationTests : IntegrationTestBase
{
    private const string ReconciliationBase = "/api/v1.0/internal/operational-audit/reconciliation";
    private const string CacheBase = "/api/v1.0/internal/operational-audit/cache";
    private const string SyncDeviceId = "cache-invalidation-device-001";

    public OperationalCacheInvalidationIntegrationTests(IntegrationPostgresFixture postgresFixture) : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Reconciliation_transition_invalidates_reconciliation_summary_cache()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        var conflictId = await SeedUnresolvedConflictAsync();
        ClearAllDiagnosticsCaches();

        var telemetry = GetCacheTelemetry();
        var invalidationsBefore = telemetry.GetSnapshot().TotalInvalidations;

        (await _client.GetAsync($"{ReconciliationBase}/summary")).EnsureSuccessStatusCode();

        var acknowledge = await _client.PostAsJsonAsync(
            $"{ReconciliationBase}/acknowledge/{conflictId}",
            new ReconciliationStatusChangeRequest { Notes = "invalidation-test" });
        acknowledge.EnsureSuccessStatusCode();

        Assert.True(telemetry.GetSnapshot().TotalInvalidations > invalidationsBefore);

        var summaryAfter = await _client.GetAsync($"{CacheBase}/summary");
        summaryAfter.EnsureSuccessStatusCode();
        var diagnostics = await summaryAfter.Content.ReadFromJsonAsync<OperationalDiagnosticsCacheDiagnosticsSummaryDto>();
        Assert.NotNull(diagnostics);
        Assert.True(diagnostics!.TotalInvalidations > 0);
    }

    [SkippableFact]
    public async Task Replay_mismatch_recording_invalidates_incident_groups_cache()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId(SyncDeviceId);
        ClearAllDiagnosticsCaches();

        const string opId = "cache-invalidation-replay-op";
        var telemetry = GetCacheTelemetry();
        var invalidationsBefore = telemetry.GetSnapshot().TotalInvalidations;

        (await _client.GetAsync("/api/v1.0/internal/operational-audit/incidents/summary")).EnsureSuccessStatusCode();
        await SeedReplayMismatchConflictAsync(opId);

        Assert.True(telemetry.GetSnapshot().TotalInvalidations > invalidationsBefore);
    }

    [SkippableFact]
    public async Task RemoveAllDiagnosticsCaches_clears_active_entries()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        (await _client.GetAsync("/api/v1.0/internal/operational-audit/resilience/summary")).EnsureSuccessStatusCode();

        using (var scope = _factory.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationalDiagnosticsCache>().RemoveAllDiagnosticsCaches();
        }

        var summary = await _client.GetAsync($"{CacheBase}/summary");
        summary.EnsureSuccessStatusCode();
        var diagnostics = await summary.Content.ReadFromJsonAsync<OperationalDiagnosticsCacheDiagnosticsSummaryDto>();
        Assert.NotNull(diagnostics);
        Assert.Equal(0, diagnostics!.ActiveEntryCount);
    }

    [SkippableFact]
    public async Task Stale_risk_recalculates_after_invalidation()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ClearAllDiagnosticsCaches();

        (await _client.GetAsync("/api/v1.0/internal/operational-audit/resilience/summary")).EnsureSuccessStatusCode();

        var before = await _client.GetAsync($"{CacheBase}/stale-risk");
        before.EnsureSuccessStatusCode();
        var staleBefore = await before.Content.ReadFromJsonAsync<OperationalDiagnosticsCacheDiagnosticsStaleRiskDto>();
        Assert.NotNull(staleBefore);

        ClearAllDiagnosticsCaches();

        var after = await _client.GetAsync($"{CacheBase}/stale-risk");
        after.EnsureSuccessStatusCode();
        var staleAfter = await after.Content.ReadFromJsonAsync<OperationalDiagnosticsCacheDiagnosticsStaleRiskDto>();

        Assert.NotNull(staleAfter);
        Assert.Empty(staleAfter!.AtRiskEntries);
        Assert.Equal(0, staleAfter.AgingEntryCount);
        Assert.Equal(0, staleAfter.NearExpiryEntryCount);
        Assert.Equal(0, staleAfter.ExpiredEntryCount);
    }

    [SkippableFact]
    public async Task Diagnostics_summary_exposes_scoped_key_metadata_after_warm()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ClearAllDiagnosticsCaches();

        (await _client.GetAsync($"{ReconciliationBase}/summary")).EnsureSuccessStatusCode();

        var response = await _client.GetAsync($"{CacheBase}/summary");
        response.EnsureSuccessStatusCode();
        var diagnostics = await response.Content.ReadFromJsonAsync<OperationalDiagnosticsCacheDiagnosticsSummaryDto>();

        Assert.NotNull(diagnostics);
        Assert.Contains(diagnostics!.Entries, e =>
            e.KeyDomain == OperationalDiagnosticsCacheKeyConstants.ReconciliationDomain
            && e.Scope == OperationalDiagnosticsCacheScopes.Global);
    }

    private async Task<Guid> SeedUnresolvedConflictAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PosDbContext>();
        var id = Guid.NewGuid();
        db.SyncConflictRecords.Add(new SyncConflictRecord
        {
            Id = id,
            DeviceId = SyncDeviceId,
            OperationId = "cache-invalidation-op",
            OperationType = "AdjustInventory",
            EntityType = "SyncOperation",
            ConflictType = SyncConflictTypes.LifecycleStateConflict,
            Reason = "invalidation integration test",
            CreatedAtUtc = DateTime.UtcNow,
            Resolved = false,
            ResolutionStatus = nameof(ReconciliationResolutionStatus.Unresolved)
        });
        await db.SaveChangesAsync();
        return id;
    }

    private async Task SeedReplayMismatchConflictAsync(string opId)
    {
        Guid ingredientId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<PosDbContext>();
            var ingredient = new Ingredient
            {
                Id = Guid.NewGuid(),
                Name = "Invalidation Ingredient",
                Unit = "kg",
                CostPerUnit = 1m,
                IsActive = true
            };
            ctx.Ingredients.Add(ingredient);
            ctx.InventoryItems.Add(new InventoryItem
            {
                Id = Guid.NewGuid(),
                IngredientId = ingredient.Id,
                CurrentStock = 10m,
                MinimumStock = 0m,
                MaximumStock = 100m,
                AverageCost = 1m,
                Unit = "kg",
                LastUpdated = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();
            ingredientId = ingredient.Id;
        }

        var adjustBody = new
        {
            deviceId = SyncDeviceId,
            operations = new[]
            {
                new
                {
                    operationId = opId,
                    type = "AdjustInventory",
                    payload = new Dictionary<string, object?>
                    {
                        ["ingredientId"] = ingredientId.ToString(),
                        ["quantity"] = "1",
                        ["reason"] = "invalidation-test"
                    }
                }
            }
        };
        (await _client.PostAsJsonAsync("/api/v1.0/sync/push", adjustBody)).EnsureSuccessStatusCode();

        var mismatchBody = new
        {
            deviceId = SyncDeviceId,
            operations = new[]
            {
                new
                {
                    operationId = opId,
                    type = "CreateOrder",
                    payload = new Dictionary<string, object?> { ["orderType"] = "DineIn" }
                }
            }
        };
        (await _client.PostAsJsonAsync("/api/v1.0/sync/push", mismatchBody)).EnsureSuccessStatusCode();
    }

    protected override async Task SeedTestDataAsync(PosDbContext context)
    {
        context.Devices.Add(new Device
        {
            Id = Guid.NewGuid(),
            DeviceId = SyncDeviceId,
            Name = "Cache Invalidation Device",
            DeviceType = "Terminal",
            IsActive = true
        });
        await context.SaveChangesAsync();
    }

    private void ClearAllDiagnosticsCaches()
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IOperationalDiagnosticsCache>().RemoveAllDiagnosticsCaches();
    }

    private IOperationalDiagnosticsCacheTelemetry GetCacheTelemetry()
    {
        using var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IOperationalDiagnosticsCacheTelemetry>();
    }
}
