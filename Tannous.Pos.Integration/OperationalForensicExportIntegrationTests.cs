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

public class OperationalForensicExportIntegrationTests : IntegrationTestBase
{
    private const string ExportBase = "/api/v1.0/internal/operational-audit/export";
    private const string SyncDeviceId = "forensic-export-device-001";

    public OperationalForensicExportIntegrationTests(IntegrationPostgresFixture postgresFixture) : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Export_by_order_returns_ascending_audit_timeline()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId(SyncDeviceId);

        var menuItemId = await GetMenuItemIdAsync();
        var orderId = await CreateOpenOrderViaSyncAsync(menuItemId, $"forensic-order-{Guid.NewGuid():N}");
        await OpenOrderAsync(orderId);
        await FinalizeViaSyncAsync(orderId, $"forensic-fin-{Guid.NewGuid():N}");

        var response = await _client.GetAsync($"{ExportBase}/order/{orderId}");
        response.EnsureSuccessStatusCode();
        var snapshot = await response.Content.ReadFromJsonAsync<OperationalForensicSnapshotDto>();
        Assert.NotNull(snapshot);
        Assert.Equal(OperationalForensicSnapshotTypes.Order, snapshot!.SnapshotType);
        Assert.NotEmpty(snapshot.AuditTimeline);
        Assert.True(snapshot.AuditTimeline.SequenceEqual(snapshot.AuditTimeline.OrderBy(i => i.TimestampUtc)));
        AssertNoSensitiveLeakage(snapshot);
    }

    [SkippableFact]
    public async Task Export_by_operation_includes_replay_mismatch_audit_and_conflict()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId(SyncDeviceId);

        const string opId = "forensic-export-mismatch-001";
        await SeedReplayMismatchConflictAsync(opId);

        var response = await _client.GetAsync($"{ExportBase}/operation/{opId}");
        response.EnsureSuccessStatusCode();
        var snapshot = await response.Content.ReadFromJsonAsync<OperationalForensicSnapshotDto>();
        Assert.NotNull(snapshot);
        Assert.Equal(OperationalForensicSnapshotTypes.Operation, snapshot!.SnapshotType);
        Assert.NotEmpty(snapshot.AuditTimeline);
        Assert.NotEmpty(snapshot.ConflictRecords);
        Assert.Contains(snapshot.ConflictRecords, c => c.ConflictType.Contains("Replay", StringComparison.OrdinalIgnoreCase));
        Assert.True(snapshot.Metadata.ContainsKey("replayReceiptCount"));
    }

    [SkippableFact]
    public async Task Export_by_conflict_includes_resolution_status_and_audit_entries()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        const string opId = "forensic-export-conflict-001";
        var conflictId = await SeedReplayMismatchConflictAsync(opId);

        var response = await _client.GetAsync($"{ExportBase}/conflict/{conflictId}");
        response.EnsureSuccessStatusCode();
        var snapshot = await response.Content.ReadFromJsonAsync<OperationalForensicSnapshotDto>();
        Assert.NotNull(snapshot);
        Assert.Equal(OperationalForensicSnapshotTypes.Conflict, snapshot!.SnapshotType);
        Assert.Contains(snapshot.ConflictRecords, c => c.Id == conflictId);
        Assert.Equal(nameof(ReconciliationResolutionStatus.Unresolved), snapshot.ConflictRecords.First(c => c.Id == conflictId).ResolutionStatus);
        Assert.NotEmpty(snapshot.AuditTimeline);
        Assert.True(snapshot.Metadata.ContainsKey("reconciliationStatuses"));
    }

    [SkippableFact]
    public async Task Export_by_device_returns_timeline_and_metadata()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId(SyncDeviceId);

        var pushBody = new
        {
            deviceId = SyncDeviceId,
            operations = new[]
            {
                new
                {
                    operationId = "forensic-device-op-001",
                    type = "CreateCustomer",
                    payload = new Dictionary<string, object?>()
                }
            }
        };
        (await _client.PostAsJsonAsync("/api/v1.0/sync/push", pushBody)).EnsureSuccessStatusCode();

        var response = await _client.GetAsync($"{ExportBase}/device/{SyncDeviceId}");
        response.EnsureSuccessStatusCode();
        var snapshot = await response.Content.ReadFromJsonAsync<OperationalForensicSnapshotDto>();
        Assert.NotNull(snapshot);
        Assert.Equal(OperationalForensicSnapshotTypes.Device, snapshot!.SnapshotType);
        Assert.NotEmpty(snapshot.AuditTimeline);
        AssertNoSensitiveLeakage(snapshot);
    }

    [SkippableFact]
    public async Task Replay_mismatch_forensic_export_shows_unresolved_reconciliation_status()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        var conflictId = await SeedReplayMismatchConflictAsync("forensic-recon-status-001");
        var response = await _client.GetAsync($"{ExportBase}/conflict/{conflictId}");
        response.EnsureSuccessStatusCode();
        var snapshot = await response.Content.ReadFromJsonAsync<OperationalForensicSnapshotDto>();
        Assert.NotNull(snapshot);
        var conflict = snapshot!.ConflictRecords.Single(c => c.Id == conflictId);
        Assert.Equal(nameof(ReconciliationResolutionStatus.Unresolved), conflict.ResolutionStatus);
    }

    [SkippableFact]
    public async Task Cashier_is_denied_forensic_export_endpoints()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync("cashier", "password");
        SetAuthHeader(token);

        var response = await _client.GetAsync($"{ExportBase}/device/{SyncDeviceId}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static void AssertNoSensitiveLeakage(OperationalForensicSnapshotDto snapshot)
    {
        foreach (var item in snapshot.AuditTimeline)
        {
            Assert.DoesNotContain("stack", item.Message, StringComparison.OrdinalIgnoreCase);
            foreach (var (key, value) in item.Metadata)
            {
                Assert.DoesNotContain("payload", key, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("stack", key, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("stack", value, StringComparison.OrdinalIgnoreCase);
            }
        }

        foreach (var (key, value) in snapshot.Metadata)
        {
            Assert.DoesNotContain("payload", key, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("stack", key, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("stack", value, StringComparison.OrdinalIgnoreCase);
        }
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
                Name = "Forensic Export Ingredient",
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
                        ["reason"] = "forensic-test"
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

    private async Task<Guid> GetMenuItemIdAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<PosDbContext>();
        return await ctx.MenuItems.Select(m => m.Id).FirstAsync();
    }

    private async Task OpenOrderAsync(Guid orderId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<PosDbContext>();
        var order = await ctx.Orders.SingleAsync(o => o.Id == orderId);
        order.Status = OrderStatus.Open;
        await ctx.SaveChangesAsync();
    }

    private async Task<Guid> CreateOpenOrderViaSyncAsync(Guid menuItemId, string opId)
    {
        var pushBody = new
        {
            deviceId = SyncDeviceId,
            operations = new[]
            {
                new
                {
                    operationId = opId,
                    type = "CreateOrder",
                    payload = new Dictionary<string, object?>
                    {
                        ["orderType"] = "DineIn",
                        ["orderLines"] = new[]
                        {
                            new { menuItemId = menuItemId.ToString(), quantity = "1", unitPrice = "10.00" }
                        }
                    }
                }
            }
        };
        var response = await _client.PostAsJsonAsync("/api/v1.0/sync/push", pushBody);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return Guid.Parse(json.GetProperty("results")[0].GetProperty("serverId").GetString()!);
    }

    private async Task FinalizeViaSyncAsync(Guid orderId, string opId)
    {
        var pushBody = new
        {
            deviceId = SyncDeviceId,
            operations = new[]
            {
                new
                {
                    operationId = opId,
                    type = "FinalizeOrder",
                    payload = new Dictionary<string, object?>
                    {
                        ["orderId"] = orderId.ToString(),
                        ["payments"] = new[]
                        {
                            new
                            {
                                paymentMethod = "Cash",
                                amount = "20.00",
                                transactionId = opId
                            }
                        }
                    }
                }
            }
        };
        (await _client.PostAsJsonAsync("/api/v1.0/sync/push", pushBody)).EnsureSuccessStatusCode();
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
            Name = "Forensic Export Device",
            DeviceType = "Terminal",
            IsActive = true
        });
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Forensic Category",
            IsActive = true,
            DisplayOrder = 1
        };
        context.Categories.Add(category);
        context.MenuItems.Add(new MenuItem
        {
            Id = Guid.NewGuid(),
            Name = "Forensic Item",
            Price = 10m,
            CategoryId = category.Id,
            IsActive = true
        });
        await context.SaveChangesAsync();
    }
}
