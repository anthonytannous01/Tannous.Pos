using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Integration;

public class OperationalIncidentCorrelationIntegrationTests : IntegrationTestBase
{
    private const string IncidentsBase = "/api/v1.0/internal/operational-audit/incidents";
    private const string ExportBase = "/api/v1.0/internal/operational-audit/export";
    private const string SyncDeviceId = "incident-correlation-device-001";

    public OperationalIncidentCorrelationIntegrationTests(IntegrationPostgresFixture postgresFixture) : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Incident_summary_returns_aggregate_counts()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        var response = await _client.GetAsync($"{IncidentsBase}/summary");
        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<OperationalIncidentSummaryDto>();
        Assert.NotNull(summary);
        Assert.Contains("nonGoals", summary!.CorrelationGuidance.Keys, StringComparer.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task Replay_mismatch_aggregates_by_operation()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        const string opId = "incident-replay-op-001";
        await SeedReplayMismatchConflictAsync(opId);

        var response = await _client.GetAsync($"{IncidentsBase}/by-operation/{opId}");
        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<OperationalIncidentPageDto>();
        Assert.NotNull(page);
        Assert.True(page!.Total >= 1);
        Assert.Contains(page.Items, i => i.OperationId == opId);
    }

    [SkippableFact]
    public async Task High_risk_endpoint_returns_items_when_replay_incidents_present()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        await SeedReplayMismatchConflictAsync("incident-high-risk-op-001");

        var response = await _client.GetAsync($"{IncidentsBase}/high-risk");
        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<OperationalIncidentPageDto>();
        Assert.NotNull(page);
    }

    [SkippableFact]
    public async Task Cascading_degradation_endpoint_returns_patterns()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        await SeedReplayMismatchConflictAsync("incident-cascade-op-001");

        var response = await _client.GetAsync($"{IncidentsBase}/cascading-degradation");
        response.EnsureSuccessStatusCode();
        var cascading = await response.Content.ReadFromJsonAsync<OperationalCascadingDegradationDto>();
        Assert.NotNull(cascading);
    }

    [SkippableFact]
    public async Task Forensic_export_includes_incident_correlation_fields()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        var conflictId = await SeedReplayMismatchConflictAsync("incident-forensic-op-001");
        var response = await _client.GetAsync($"{ExportBase}/conflict/{conflictId}");
        response.EnsureSuccessStatusCode();
        var snapshot = await response.Content.ReadFromJsonAsync<OperationalForensicSnapshotDto>();
        Assert.NotNull(snapshot);
        Assert.False(string.IsNullOrWhiteSpace(snapshot!.CorrelatedIncidentRisk));
        Assert.False(string.IsNullOrWhiteSpace(snapshot.IncidentCorrelationSummary));
    }

    [SkippableFact]
    public async Task Cashier_is_denied_incident_endpoints()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync("cashier", "password");
        SetAuthHeader(token);

        var response = await _client.GetAsync($"{IncidentsBase}/summary");
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
                Name = "Incident Ingredient",
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
                        ["reason"] = "incident-test"
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
            Name = "Incident Device",
            DeviceType = "Terminal",
            IsActive = true
        });
        await context.SaveChangesAsync();
    }
}
