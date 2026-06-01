using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.Audit;

namespace Tannous.Pos.Integration;

public class OperationalForensicCacheIntegrationTests : IntegrationTestBase
{
    private const string ExportBase = "/api/v1.0/internal/operational-audit/export";
    private const string SyncDeviceId = "forensic-cache-device-001";

    public OperationalForensicCacheIntegrationTests(IntegrationPostgresFixture postgresFixture) : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Forensic_export_returns_live_snapshot_and_compact_summary()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId(SyncDeviceId);

        const string opId = "forensic-cache-op-001";
        await SeedReplayMismatchConflictAsync(opId);
        ResetOperationalDiagnosticsCaches();

        var telemetry = GetCacheTelemetry();
        var reconciliationHitsBefore = GetCategoryHits(telemetry, OperationalDiagnosticsCacheCategories.ReconciliationSummary);

        var response = await _client.GetAsync($"{ExportBase}/operation/{opId}");
        response.EnsureSuccessStatusCode();
        var snapshot = await response.Content.ReadFromJsonAsync<OperationalForensicSnapshotDto>();

        Assert.NotNull(snapshot);
        Assert.NotEmpty(snapshot!.AuditTimeline);
        Assert.NotEmpty(snapshot.ConflictRecords);
        Assert.NotNull(snapshot.CompactSummary);
        Assert.True(snapshot.CompactSummary!.ConflictCount > 0);
        Assert.True(snapshot.CompactSummary.AuditRecordCount > 0);
        Assert.False(string.IsNullOrWhiteSpace(snapshot.CompactSummary.CorrelatedIncidentRisk));
        Assert.False(string.IsNullOrWhiteSpace(snapshot.CompactSummary.OperationalPressureSummary));
        Assert.True(
            GetCategoryHits(telemetry, OperationalDiagnosticsCacheCategories.ReconciliationSummary) > reconciliationHitsBefore);
    }

    [SkippableFact]
    public async Task Repeated_forensic_export_reuses_upstream_cache_on_second_call()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId(SyncDeviceId);

        const string opId = "forensic-cache-op-002";
        await SeedReplayMismatchConflictAsync(opId);
        ResetOperationalDiagnosticsCaches();

        var telemetry = GetCacheTelemetry();
        var incidentHitsBefore = GetCategoryHits(telemetry, OperationalDiagnosticsCacheCategories.IncidentGroups);

        (await _client.GetAsync($"{ExportBase}/operation/{opId}")).EnsureSuccessStatusCode();
        (await _client.GetAsync($"{ExportBase}/operation/{opId}")).EnsureSuccessStatusCode();

        Assert.True(
            GetCategoryHits(telemetry, OperationalDiagnosticsCacheCategories.IncidentGroups) > incidentHitsBefore);
    }

    private async Task SeedReplayMismatchConflictAsync(string opId)
    {
        Guid ingredientId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<Tannous.Pos.Infrastructure.Data.PosDbContext>();
            var ingredient = new Tannous.Pos.Domain.Entities.Ingredient
            {
                Id = Guid.NewGuid(),
                Name = "Forensic Cache Ingredient",
                Unit = "kg",
                CostPerUnit = 1m,
                IsActive = true
            };
            ctx.Ingredients.Add(ingredient);
            ctx.InventoryItems.Add(new Tannous.Pos.Domain.Entities.InventoryItem
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
                        ["reason"] = "forensic-cache-test"
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

    protected override async Task SeedTestDataAsync(Tannous.Pos.Infrastructure.Data.PosDbContext context)
    {
        context.Devices.Add(new Tannous.Pos.Domain.Entities.Device
        {
            Id = Guid.NewGuid(),
            DeviceId = SyncDeviceId,
            Name = "Forensic Cache Device",
            DeviceType = "Terminal",
            IsActive = true
        });
        await context.SaveChangesAsync();
    }

    private IOperationalDiagnosticsCacheTelemetry GetCacheTelemetry() =>
        GetOperationalDiagnosticsCacheTelemetry();

    private static long GetCategoryHits(IOperationalDiagnosticsCacheTelemetry telemetry, string category) =>
        GetOperationalCacheCategoryHits(telemetry, category);
}
