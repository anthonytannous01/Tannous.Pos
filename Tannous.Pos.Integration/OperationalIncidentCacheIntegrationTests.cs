using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Integration;

public class OperationalIncidentCacheIntegrationTests : IntegrationTestBase
{
    private const string IncidentsBase = "/api/v1.0/internal/operational-audit/incidents";
    private const string SyncDeviceId = "incident-cache-device-001";

    public OperationalIncidentCacheIntegrationTests(IntegrationPostgresFixture postgresFixture) : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Incident_summary_reuses_cached_groups_on_second_call()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ClearIncidentGroupsCache();

        var telemetry = GetCacheTelemetry();
        var category = OperationalDiagnosticsCacheCategories.IncidentGroups;
        var hitsBefore = GetCategoryHits(telemetry, category);
        var missesBefore = GetCategoryMisses(telemetry, category);

        (await _client.GetAsync($"{IncidentsBase}/summary")).EnsureSuccessStatusCode();
        (await _client.GetAsync($"{IncidentsBase}/summary")).EnsureSuccessStatusCode();

        Assert.True(GetCategoryMisses(telemetry, category) > missesBefore);
        Assert.True(GetCategoryHits(telemetry, category) > hitsBefore);
    }

    [SkippableFact]
    public async Task Cascading_degradation_reuses_cached_incident_groups()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        ClearIncidentGroupsCache();

        var telemetry = GetCacheTelemetry();
        var category = OperationalDiagnosticsCacheCategories.IncidentGroups;
        var hitsBefore = GetCategoryHits(telemetry, category);

        (await _client.GetAsync($"{IncidentsBase}/summary")).EnsureSuccessStatusCode();
        (await _client.GetAsync($"{IncidentsBase}/cascading-degradation")).EnsureSuccessStatusCode();

        Assert.True(GetCategoryHits(telemetry, category) > hitsBefore);
    }

    [SkippableFact]
    public async Task Filtered_by_operation_derives_from_cached_groups()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        const string opId = "incident-cache-op-001";
        await SeedReplayMismatchConflictAsync(opId);
        ClearIncidentGroupsCache();

        var telemetry = GetCacheTelemetry();
        var category = OperationalDiagnosticsCacheCategories.IncidentGroups;
        var hitsBefore = GetCategoryHits(telemetry, category);

        var filtered = await _client.GetAsync($"{IncidentsBase}/by-operation/{opId}");
        filtered.EnsureSuccessStatusCode();
        var page = await filtered.Content.ReadFromJsonAsync<OperationalIncidentPageDto>();

        (await _client.GetAsync($"{IncidentsBase}/summary")).EnsureSuccessStatusCode();

        Assert.True(GetCategoryHits(telemetry, category) > hitsBefore, "Summary should reuse cached incident groups.");
        Assert.NotNull(page);
        Assert.Contains(page!.Items, i => i.OperationId == opId);
    }

    private async Task SeedReplayMismatchConflictAsync(string opId)
    {
        SetDeviceId(SyncDeviceId);

        Guid ingredientId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<PosDbContext>();
            var ingredient = new Ingredient
            {
                Id = Guid.NewGuid(),
                Name = "Incident Cache Ingredient",
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
                        ["reason"] = "incident-cache-test"
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
            Name = "Incident Cache Device",
            DeviceType = "Terminal",
            IsActive = true
        });
        await context.SaveChangesAsync();
    }

    private void ClearIncidentGroupsCache()
    {
        using var scope = _factory.Services.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<IOperationalDiagnosticsCache>();
        cache.Remove(
            OperationalDiagnosticsCacheConstants.IncidentGroupsCacheKey,
            OperationalDiagnosticsCacheCategories.IncidentGroups);
    }

    private IOperationalDiagnosticsCacheTelemetry GetCacheTelemetry()
    {
        using var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IOperationalDiagnosticsCacheTelemetry>();
    }

    private static long GetCategoryHits(IOperationalDiagnosticsCacheTelemetry telemetry, string category) =>
        telemetry.GetSnapshot().ByCategory.TryGetValue(category, out var stats) ? stats.Hits : 0;

    private static long GetCategoryMisses(IOperationalDiagnosticsCacheTelemetry telemetry, string category) =>
        telemetry.GetSnapshot().ByCategory.TryGetValue(category, out var stats) ? stats.Misses : 0;
}
