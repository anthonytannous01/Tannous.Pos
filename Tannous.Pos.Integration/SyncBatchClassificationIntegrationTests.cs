using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Integration;

public class SyncBatchClassificationIntegrationTests : IntegrationTestBase
{
    public SyncBatchClassificationIntegrationTests(IntegrationPostgresFixture postgresFixture) : base(postgresFixture)
    {
    }

    protected override async Task SeedTestDataAsync(PosDbContext context)
    {
        context.Devices.Add(new Device
        {
            Id = Guid.NewGuid(),
            DeviceId = "batch-class-device-001",
            Name = "Batch Class Device",
            DeviceType = "Terminal",
            IsActive = true
        });

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Batch Class Category",
            IsActive = true,
            DisplayOrder = 1
        };
        context.Categories.Add(category);

        context.MenuItems.Add(new MenuItem
        {
            Id = Guid.NewGuid(),
            Name = "Batch Class Item",
            Price = 5.00m,
            CategoryId = category.Id,
            IsActive = true
        });

        await context.SaveChangesAsync();
    }

    [SkippableFact]
    public async Task Mixed_batch_success_replay_and_validation_failure_stable_counts_no_duplicate_orders()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId("batch-class-device-001");

        Guid menuItemId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<PosDbContext>();
            menuItemId = await ctx.MenuItems.Select(x => x.Id).FirstAsync();
        }

        const string createOpId = "batch-mix-create-001";
        var pushBody = new
        {
            deviceId = "batch-class-device-001",
            operations = new object[]
            {
                new
                {
                    operationId = createOpId,
                    type = "CreateOrder",
                    payload = new Dictionary<string, object?>
                    {
                        ["orderType"] = "DineIn",
                        ["orderLines"] = new[]
                        {
                            new { menuItemId = menuItemId.ToString(), quantity = "1", unitPrice = "5.00" }
                        }
                    }
                },
                new
                {
                    operationId = createOpId,
                    type = "CreateOrder",
                    payload = new Dictionary<string, object?>
                    {
                        ["orderType"] = "DineIn",
                        ["orderLines"] = new[]
                        {
                            new { menuItemId = menuItemId.ToString(), quantity = "1", unitPrice = "5.00" }
                        }
                    }
                },
                new
                {
                    operationId = "batch-mix-bad-001",
                    type = "CreateOrder",
                    payload = new Dictionary<string, object?>()
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/v1.0/sync/push", pushBody);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var results = json.GetProperty("results");
        Assert.Equal(3, results.GetArrayLength());

        var r0 = results[0];
        var r1 = results[1];
        var r2 = results[2];
        Assert.True(r0.GetProperty("success").GetBoolean());
        Assert.True(r1.GetProperty("success").GetBoolean());
        Assert.False(r2.GetProperty("success").GetBoolean());
        Assert.Equal(
            r0.GetProperty("serverId").GetString(),
            r1.GetProperty("serverId").GetString());

        var successCount = 0;
        var failCount = 0;
        for (var i = 0; i < results.GetArrayLength(); i++)
        {
            if (results[i].GetProperty("success").GetBoolean())
                successCount++;
            else
                failCount++;
        }
        Assert.Equal(2, successCount);
        Assert.Equal(1, failCount);

        await using var verify = _factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<PosDbContext>();
        var orderId = Guid.Parse(r0.GetProperty("serverId").GetString()!);
        Assert.Equal(1, await db.Orders.CountAsync(o => o.Id == orderId));
        Assert.Single(await db.SyncOperationReceipts.Where(r => r.OperationId == createOpId).ToListAsync());
    }

    [SkippableFact]
    public async Task Partial_batch_inventory_replay_maintains_single_movement_and_stock()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId("batch-class-device-001");

        Guid ingredientId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<PosDbContext>();
            var ingredient = new Ingredient
            {
                Id = Guid.NewGuid(),
                Name = "Batch Class Ingredient",
                Unit = "kg",
                CostPerUnit = 2.00m,
                IsActive = true
            };
            ctx.Ingredients.Add(ingredient);
            ctx.InventoryItems.Add(new InventoryItem
            {
                Id = Guid.NewGuid(),
                IngredientId = ingredient.Id,
                CurrentStock = 40.0m,
                MinimumStock = 1.0m,
                MaximumStock = 100.0m,
                AverageCost = 2.00m,
                Unit = "kg",
                LastUpdated = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();
            ingredientId = ingredient.Id;
        }

        const string adjOpId = "batch-inv-replay-001";
        var pushBody = new
        {
            deviceId = "batch-class-device-001",
            operations = new object[]
            {
                new
                {
                    operationId = adjOpId,
                    type = "AdjustInventory",
                    payload = new Dictionary<string, object?>
                    {
                        ["ingredientId"] = ingredientId.ToString(),
                        ["quantity"] = "5",
                        ["reason"] = "batch-first"
                    }
                },
                new
                {
                    operationId = adjOpId,
                    type = "AdjustInventory",
                    payload = new Dictionary<string, object?>
                    {
                        ["ingredientId"] = ingredientId.ToString(),
                        ["quantity"] = "5",
                        ["reason"] = "batch-replay"
                    }
                },
                new
                {
                    operationId = "batch-inv-bad-001",
                    type = "AdjustInventory",
                    payload = new Dictionary<string, object?>
                    {
                        ["ingredientId"] = ingredientId.ToString(),
                        ["quantity"] = "not-a-number",
                        ["reason"] = "bad"
                    }
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/v1.0/sync/push", pushBody);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var results = json.GetProperty("results");
        Assert.True(results[0].GetProperty("success").GetBoolean());
        Assert.True(results[1].GetProperty("success").GetBoolean());
        Assert.False(results[2].GetProperty("success").GetBoolean());

        await using var verify = _factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<PosDbContext>();
        var movements = await db.InventoryMovements
            .Where(m => m.IngredientId == ingredientId && m.MovementType == InventoryMovementType.Adjustment)
            .ToListAsync();
        Assert.Single(movements);
        var stock = await db.InventoryItems.Where(ii => ii.IngredientId == ingredientId).Select(ii => ii.CurrentStock).SingleAsync();
        Assert.Equal(45.0m, stock);
        Assert.Single(await db.SyncOperationReceipts.Where(r => r.OperationId == adjOpId).ToListAsync());
    }

    [SkippableFact]
    public async Task Mixed_batch_continues_after_placeholder_and_unknown_operation()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId("batch-class-device-001");

        var pushBody = new
        {
            deviceId = "batch-class-device-001",
            operations = new object[]
            {
                new { operationId = "batch-ph-001", type = "OpenShift", payload = new Dictionary<string, object?>() },
                new { operationId = "batch-unknown-001", type = "NotARealOp", payload = new Dictionary<string, object?>() }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/v1.0/sync/push", pushBody);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var results = json.GetProperty("results");
        Assert.Equal(2, results.GetArrayLength());
        Assert.True(results[0].GetProperty("success").GetBoolean());
        Assert.False(results[1].GetProperty("success").GetBoolean());
    }

    [SkippableFact]
    public async Task Mixed_batch_open_shift_replay_and_create_order_validation_failure()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId("batch-class-device-001");

        const string shiftOpId = "batch-shift-replay-mix-001";
        var pushBody = new
        {
            deviceId = "batch-class-device-001",
            operations = new object[]
            {
                new { operationId = shiftOpId, type = "OpenShift", payload = new Dictionary<string, object?>() },
                new { operationId = shiftOpId, type = "OpenShift", payload = new Dictionary<string, object?>() },
                new
                {
                    operationId = "batch-shift-bad-order-001",
                    type = "CreateOrder",
                    payload = new Dictionary<string, object?>()
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/v1.0/sync/push", pushBody);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var results = json.GetProperty("results");
        Assert.Equal(3, results.GetArrayLength());
        Assert.True(results[0].GetProperty("success").GetBoolean());
        Assert.True(results[1].GetProperty("success").GetBoolean());
        Assert.False(results[2].GetProperty("success").GetBoolean());
        Assert.Equal(
            results[0].GetProperty("serverId").GetString(),
            results[1].GetProperty("serverId").GetString());

        await using var verify = _factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<PosDbContext>();
        Assert.Single(await db.SyncOperationReceipts.Where(r => r.OperationId == shiftOpId).ToListAsync());
        Assert.Equal(0, await db.Orders.CountAsync());
    }

    [SkippableFact]
    public async Task Mixed_batch_replay_placeholder_and_first_run_create_customer()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId("batch-class-device-001");

        const string customerOpId = "batch-cust-replay-mix-001";
        var firstPush = new
        {
            deviceId = "batch-class-device-001",
            operations = new object[]
            {
                new { operationId = customerOpId, type = "CreateCustomer", payload = new Dictionary<string, object?>() }
            }
        };
        var first = await _client.PostAsJsonAsync("/api/v1.0/sync/push", firstPush);
        first.EnsureSuccessStatusCode();
        var firstJson = await first.Content.ReadFromJsonAsync<JsonElement>();
        var firstServerId = firstJson.GetProperty("results")[0].GetProperty("serverId").GetString();

        var mixedPush = new
        {
            deviceId = "batch-class-device-001",
            operations = new object[]
            {
                new { operationId = customerOpId, type = "CreateCustomer", payload = new Dictionary<string, object?>() },
                new { operationId = "batch-cust-new-001", type = "CreateCustomer", payload = new Dictionary<string, object?>() }
            }
        };
        var response = await _client.PostAsJsonAsync("/api/v1.0/sync/push", mixedPush);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var results = json.GetProperty("results");
        Assert.Equal(2, results.GetArrayLength());
        Assert.True(results[0].GetProperty("success").GetBoolean());
        Assert.True(results[1].GetProperty("success").GetBoolean());
        Assert.Equal(firstServerId, results[0].GetProperty("serverId").GetString());

        await using var verify = _factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<PosDbContext>();
        Assert.Equal(2, await db.SyncOperationReceipts.CountAsync(r =>
            r.DeviceId == "batch-class-device-001" &&
            (r.OperationId == customerOpId || r.OperationId == "batch-cust-new-001")));
    }
}
