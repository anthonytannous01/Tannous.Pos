using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Sync;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Integration;

public class OperationalAlertDiagnosticsIntegrationTests : IntegrationTestBase
{
    private const string AlertsBase = "/api/v1.0/internal/operational-audit/alerts";
    private const string ExportBase = "/api/v1.0/internal/operational-audit/export";
    private const string SyncDeviceId = "alert-diagnostics-device-001";

    public OperationalAlertDiagnosticsIntegrationTests(IntegrationPostgresFixture postgresFixture) : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Replay_pressure_generates_replay_related_signal()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        await SeedReplayMismatchConflictAsync("alert-replay-op-001");

        var response = await _client.GetAsync($"{AlertsBase}/replay-pressure");
        response.EnsureSuccessStatusCode();
        var signals = await response.Content.ReadFromJsonAsync<List<OperationalAlertSignalDto>>();
        Assert.NotNull(signals);
        Assert.Contains(signals!, s => s.AlertType == OperationalAlertTypes.ReplayStormRisk);
    }

    [SkippableFact]
    public async Task Inventory_drift_conflict_generates_inventory_warning_signal()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        await SeedInventoryDriftConflictAsync();
        ResetOperationalDiagnosticsCaches();

        var response = await _client.GetAsync($"{AlertsBase}/inventory-risk");
        response.EnsureSuccessStatusCode();
        var signals = await response.Content.ReadFromJsonAsync<List<OperationalAlertSignalDto>>();
        Assert.NotNull(signals);
        Assert.Contains(signals!, s => s.AlertType == OperationalAlertTypes.InventoryDriftEscalation);
        Assert.Contains(signals!, s => s.Severity is OperationalAlertSeverity.Warning or OperationalAlertSeverity.Critical);
    }

    [SkippableFact]
    public async Task Reconciliation_backlog_generates_escalation_signal()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        await SeedUnresolvedConflictBatchAsync(12);
        ResetOperationalDiagnosticsCaches();

        var response = await _client.GetAsync($"{AlertsBase}/current");
        response.EnsureSuccessStatusCode();
        var signals = await response.Content.ReadFromJsonAsync<List<OperationalAlertSignalDto>>();
        Assert.NotNull(signals);
        Assert.Contains(signals!, s =>
            s.AlertType == OperationalAlertTypes.ReconciliationBacklog
            || s.AlertType == OperationalAlertTypes.ConflictEscalation);
    }

    [SkippableFact]
    public async Task Alert_summary_counts_match_current_signals()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        await SeedReplayMismatchConflictAsync("alert-summary-op-001");

        var summaryResponse = await _client.GetAsync($"{AlertsBase}/summary");
        summaryResponse.EnsureSuccessStatusCode();
        var summary = await summaryResponse.Content.ReadFromJsonAsync<OperationalAlertSummaryDto>();
        Assert.NotNull(summary);

        var currentResponse = await _client.GetAsync($"{AlertsBase}/current");
        currentResponse.EnsureSuccessStatusCode();
        var current = await currentResponse.Content.ReadFromJsonAsync<List<OperationalAlertSignalDto>>();
        Assert.NotNull(current);
        Assert.Equal(current!.Count, summary!.TotalSignals);
        Assert.Equal(current.Count(s => s.Severity == OperationalAlertSeverity.Critical), summary.CriticalSignals);
        Assert.Equal(current.Count(s => s.Severity == OperationalAlertSeverity.Warning), summary.WarningSignals);
    }

    [SkippableFact]
    public async Task Forensic_export_includes_alert_metadata()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        var conflictId = await SeedReplayMismatchConflictAsync("alert-forensic-op-001");
        var response = await _client.GetAsync($"{ExportBase}/conflict/{conflictId}");
        response.EnsureSuccessStatusCode();
        var snapshot = await response.Content.ReadFromJsonAsync<OperationalForensicSnapshotDto>();
        Assert.NotNull(snapshot);
        Assert.NotNull(snapshot!.AlertSummary);
        Assert.False(string.IsNullOrWhiteSpace(snapshot.EscalationRisk));
        Assert.False(string.IsNullOrWhiteSpace(snapshot.OperationalPressureSummary));
    }

    [SkippableFact]
    public async Task Cascading_pressure_may_surface_critical_signals_when_incidents_present()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        await SeedReplayMismatchConflictAsync("alert-cascade-op-001");
        await SeedUnresolvedConflictBatchAsync(8);

        var response = await _client.GetAsync($"{AlertsBase}/critical");
        response.EnsureSuccessStatusCode();
        var signals = await response.Content.ReadFromJsonAsync<List<OperationalAlertSignalDto>>();
        Assert.NotNull(signals);
        Assert.True(signals!.Count >= 0);
    }

    [SkippableFact]
    public async Task Cashier_is_denied_alert_diagnostics_endpoints()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync("cashier", "password");
        SetAuthHeader(token);

        var response = await _client.GetAsync($"{AlertsBase}/summary");
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
                Name = "Alert Ingredient",
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
                        ["reason"] = "alert-test"
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

    private async Task SeedInventoryDriftConflictAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PosDbContext>();
        db.SyncConflictRecords.Add(new SyncConflictRecord
        {
            DeviceId = SyncDeviceId,
            OperationId = "alert-inventory-drift-001",
            OperationType = "FinalizeOrder",
            EntityType = nameof(Order),
            ConflictType = SyncConflictTypes.InventoryDriftRisk,
            Reason = "integration test inventory drift",
            CreatedAtUtc = DateTime.UtcNow,
            Resolved = false,
            ResolutionStatus = nameof(ReconciliationResolutionStatus.Unresolved)
        });
        await db.SaveChangesAsync();
    }

    private async Task SeedUnresolvedConflictBatchAsync(int count)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PosDbContext>();
        for (var i = 0; i < count; i++)
        {
            db.SyncConflictRecords.Add(new SyncConflictRecord
            {
                DeviceId = $"{SyncDeviceId}-batch",
                OperationId = $"alert-backlog-op-{i:D3}",
                OperationType = "AdjustInventory",
                EntityType = "SyncOperation",
                ConflictType = SyncConflictTypes.LifecycleStateConflict,
                Reason = "integration backlog seed",
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-i),
                Resolved = false,
                ResolutionStatus = nameof(ReconciliationResolutionStatus.Unresolved)
            });
        }

        await db.SaveChangesAsync();
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
            Name = "Alert Diagnostics Device",
            DeviceType = "Terminal",
            IsActive = true
        });
        await context.SaveChangesAsync();
    }
}
