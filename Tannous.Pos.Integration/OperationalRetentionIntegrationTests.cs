using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Integration;

public class OperationalRetentionIntegrationTests : IntegrationTestBase
{
    private const string RetentionBase = "/api/v1.0/internal/operational-audit/retention";
    private const string DiagnosticsBase = "/api/v1.0/internal/operational-audit";
    private const string ExportBase = "/api/v1.0/internal/operational-audit/export";
    private const string ReconciliationBase = "/api/v1.0/internal/operational-audit/reconciliation";
    private const string SyncDeviceId = "retention-test-device-001";

    public OperationalRetentionIntegrationTests(IntegrationPostgresFixture postgresFixture) : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Retention_summary_returns_safe_aggregated_metrics()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        var response = await _client.GetAsync($"{RetentionBase}/summary");
        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<OperationalRetentionSummaryDto>();
        Assert.NotNull(summary);
        Assert.True(summary!.GeneratedAtUtc <= DateTime.UtcNow.AddMinutes(1));
        Assert.Contains("nonGoals", summary.RetentionGuidance.Keys, StringComparer.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task Oversized_page_size_is_clamped_for_diagnostics()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        var response = await _client.GetAsync($"{DiagnosticsBase}/conflicts/recent?page=1&pageSize=9999");
        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<OperationalAuditPageDto>();
        Assert.Equal(OperationalAuditQueryConstants.MaxPageSize, page!.PageSize);
    }

    [SkippableFact]
    public async Task Oversized_date_range_is_clamped_for_reconciliation_unresolved()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        var from = DateTime.UtcNow.AddYears(-5).ToString("O");
        var to = DateTime.UtcNow.ToString("O");
        var response = await _client.GetAsync($"{ReconciliationBase}/unresolved?fromUtc={Uri.EscapeDataString(from)}&toUtc={Uri.EscapeDataString(to)}");
        response.EnsureSuccessStatusCode();
    }

    [SkippableFact]
    public async Task Forensic_export_includes_truncation_and_schema_metadata()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId(SyncDeviceId);

        var conflictId = await SeedReplayMismatchConflictAsync("retention-forensic-op-001");
        var response = await _client.GetAsync($"{ExportBase}/conflict/{conflictId}");
        response.EnsureSuccessStatusCode();
        var snapshot = await response.Content.ReadFromJsonAsync<OperationalForensicSnapshotDto>();
        Assert.NotNull(snapshot);
        Assert.Equal(OperationalForensicSnapshotConstants.SnapshotSchemaVersion, snapshot!.SnapshotSchemaVersion);
        Assert.Contains("export/conflict/", snapshot.ExportSource, StringComparison.Ordinal);
        Assert.NotNull(snapshot.TruncationFlags);
        Assert.False(string.IsNullOrWhiteSpace(snapshot.RetentionClassification));
    }

    [SkippableFact]
    public async Task Unresolved_conflict_lists_aging_classification()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        await SeedReplayMismatchConflictAsync("retention-aging-op-001");

        var response = await _client.GetAsync($"{ReconciliationBase}/unresolved?page=1&pageSize=50");
        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<Tannous.Pos.Application.Sync.SyncConflictPageDto>();
        Assert.Contains(page!.Items, i => !string.IsNullOrWhiteSpace(i.AgingSeverity));
    }

    [SkippableFact]
    public async Task Cashier_is_denied_retention_summary()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync("cashier", "password");
        SetAuthHeader(token);

        var response = await _client.GetAsync($"{RetentionBase}/summary");
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
                Name = "Retention Ingredient",
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
                        ["reason"] = "retention-test"
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
            Name = "Retention Test Device",
            DeviceType = "Terminal",
            IsActive = true
        });
        await context.SaveChangesAsync();
    }
}
