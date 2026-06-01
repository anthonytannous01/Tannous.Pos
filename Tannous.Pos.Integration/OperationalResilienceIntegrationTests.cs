using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Integration;

public class OperationalResilienceIntegrationTests : IntegrationTestBase
{
    private const string ResilienceBase = "/api/v1.0/internal/operational-audit/resilience";
    private const string ExportBase = "/api/v1.0/internal/operational-audit/export";
    private const string DiagnosticsBase = "/api/v1.0/internal/operational-audit";
    private const string RetentionBase = "/api/v1.0/internal/operational-audit/retention";
    private const string SyncDeviceId = "resilience-test-device-001";

    public OperationalResilienceIntegrationTests(IntegrationPostgresFixture postgresFixture) : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Resilience_summary_returns_degraded_mode_fields()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        var response = await _client.GetAsync($"{ResilienceBase}/summary");
        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<OperationalResilienceSummaryDto>();
        Assert.NotNull(summary);
        Assert.False(string.IsNullOrWhiteSpace(summary!.PrimaryDegradedMode));
        Assert.NotEmpty(summary.ResilienceGuidance);
    }

    [SkippableFact]
    public async Task Degraded_modes_endpoint_lists_known_modes()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        var response = await _client.GetAsync($"{ResilienceBase}/degraded-modes");
        response.EnsureSuccessStatusCode();
        var modes = await response.Content.ReadFromJsonAsync<OperationalDegradedModesDto>();
        Assert.Contains(modes!.Modes, m => m.Mode == OperationalDegradedModeTypes.Normal);
        Assert.Contains(modes.Modes, m => m.Mode == OperationalDegradedModeTypes.ReplayStormRisk);
    }

    [SkippableFact]
    public async Task Pressure_indicators_reflect_clamped_diagnostics_query()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        var from = DateTime.UtcNow.AddYears(-5).ToString("O");
        var to = DateTime.UtcNow.ToString("O");
        (await _client.GetAsync($"{DiagnosticsBase}/conflicts/recent?fromUtc={Uri.EscapeDataString(from)}&toUtc={Uri.EscapeDataString(to)}&pageSize=9999"))
            .EnsureSuccessStatusCode();

        var response = await _client.GetAsync($"{ResilienceBase}/pressure-indicators");
        response.EnsureSuccessStatusCode();
        var indicators = await response.Content.ReadFromJsonAsync<OperationalPressureIndicatorsDto>();
        Assert.True(
            indicators!.Indicators.GetValueOrDefault("largeRangeDiagnosticsQuery")
            || indicators.Indicators.GetValueOrDefault("excessivePaginationRequest"));
    }

    [SkippableFact]
    public async Task Replay_risk_summary_visible_after_replay_mismatch_seed()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        await SeedReplayMismatchConflictAsync("resilience-replay-op-001");

        var response = await _client.GetAsync($"{ResilienceBase}/replay-risk-summary");
        response.EnsureSuccessStatusCode();
        var replay = await response.Content.ReadFromJsonAsync<OperationalReplayRiskSummaryDto>();
        Assert.NotNull(replay);
        Assert.True(replay!.TotalReplayReceiptCount >= 1);
    }

    [SkippableFact]
    public async Task Forensic_export_includes_export_pressure_classification()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        var conflictId = await SeedReplayMismatchConflictAsync("resilience-export-op-001");
        var response = await _client.GetAsync($"{ExportBase}/conflict/{conflictId}");
        response.EnsureSuccessStatusCode();
        var snapshot = await response.Content.ReadFromJsonAsync<OperationalForensicSnapshotDto>();
        Assert.False(string.IsNullOrWhiteSpace(snapshot!.ExportPressureClassification));
        Assert.False(string.IsNullOrWhiteSpace(snapshot.TruncationSeverity));
    }

    [SkippableFact]
    public async Task Retention_summary_includes_resilience_enrichment()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        var response = await _client.GetAsync($"{RetentionBase}/summary");
        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<OperationalRetentionSummaryDto>();
        Assert.False(string.IsNullOrWhiteSpace(summary!.PrimaryDegradedMode));
        Assert.False(string.IsNullOrWhiteSpace(summary.ReconciliationBacklogSeverity));
    }

    [SkippableFact]
    public async Task Cashier_is_denied_resilience_endpoints()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync("cashier", "password");
        SetAuthHeader(token);

        var response = await _client.GetAsync($"{ResilienceBase}/summary");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<Guid> SeedReplayMismatchConflictAsync(string opId)
    {
        SetDeviceId(SyncDeviceId);

        Guid ingredientId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<PosDbContext>();
            var ingredient = new Ingredient
            {
                Id = Guid.NewGuid(),
                Name = "Resilience Ingredient",
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
                MinimumStock = 0,
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
                        ["reason"] = "resilience-test"
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

        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<PosDbContext>();
        return await db.SyncConflictRecords
            .Where(r => r.DeviceId == SyncDeviceId && r.OperationId == opId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => r.Id)
            .FirstAsync();
    }

    protected override async Task SeedTestDataAsync(PosDbContext context)
    {
        context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Username = "cashier",
            NormalizedUsername = "CASHIER",
            Email = "cashier@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            Role = Role.Cashier,
            FirstName = "Test",
            LastName = "Cashier",
            IsActive = true
        });
        context.Devices.Add(new Device
        {
            Id = Guid.NewGuid(),
            DeviceId = SyncDeviceId,
            Name = "Resilience Device",
            DeviceType = "Terminal",
            IsActive = true
        });
        await context.SaveChangesAsync();
    }
}
