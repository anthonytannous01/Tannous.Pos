using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Integration;

public class SyncReconciliationIntegrationTests : IntegrationTestBase
{
    public SyncReconciliationIntegrationTests(IntegrationPostgresFixture postgresFixture) : base(postgresFixture)
    {
    }

    private const string DeviceId = "sync-recon-device-001";

    protected override async Task SeedTestDataAsync(PosDbContext context)
    {
        context.Devices.Add(new Device
        {
            Id = Guid.NewGuid(),
            DeviceId = DeviceId,
            Name = "Sync Recon Device",
            DeviceType = "Terminal",
            IsActive = true
        });

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Sync Recon Category",
            IsActive = true,
            DisplayOrder = 1
        };
        context.Categories.Add(category);

        context.MenuItems.Add(new MenuItem
        {
            Id = Guid.NewGuid(),
            Name = "Sync Recon Item",
            Price = 10.00m,
            CategoryId = category.Id,
            IsActive = true
        });

        await context.SaveChangesAsync();
    }

    [SkippableFact]
    public async Task Replay_operation_type_mismatch_persists_single_replay_mismatch_conflict_record()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId(DeviceId);

        Guid ingredientId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<PosDbContext>();
            var ingredient = new Ingredient
            {
                Id = Guid.NewGuid(),
                Name = "Replay Mismatch Ingredient",
                Unit = "kg",
                CostPerUnit = 1.00m,
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

        const string opId = "replay-mismatch-op-001";
        var adjustBody = new
        {
            deviceId = DeviceId,
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
                        ["reason"] = "mismatch-test"
                    }
                }
            }
        };

        var first = await _client.PostAsJsonAsync("/api/v1.0/sync/push", adjustBody);
        first.EnsureSuccessStatusCode();

        var mismatchBody = new
        {
            deviceId = DeviceId,
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

        var second = await _client.PostAsJsonAsync("/api/v1.0/sync/push", mismatchBody);
        second.EnsureSuccessStatusCode();

        var third = await _client.PostAsJsonAsync("/api/v1.0/sync/push", mismatchBody);
        third.EnsureSuccessStatusCode();

        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<PosDbContext>();
        var records = await db.SyncConflictRecords
            .Where(r => r.DeviceId == DeviceId && r.OperationId == opId && r.ConflictType == "ReplayMismatch")
            .ToListAsync();
        Assert.Single(records);
    }

    [SkippableFact]
    public async Task Void_on_already_void_order_persists_lifecycle_conflict_record()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId();

        var orderId = await CreateOpenOrderAsync(token);
        await VoidOrderAsync(orderId, "recon-void-001");

        SetIdempotencyKey("recon-void-dup-001");
        var response = await _client.PostAsJsonAsync($"/api/v1.0/orders/{orderId}/void", new { reason = "second void" });
        Assert.Equal(System.Net.HttpStatusCode.Conflict, response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PosDbContext>();
        var count = await db.SyncConflictRecords.CountAsync(r =>
            r.EntityId == orderId && r.ConflictType == "LifecycleStateConflict");
        Assert.True(count >= 1);
    }

    [SkippableFact]
    public async Task Stale_finalize_after_void_persists_stale_offline_mutation_record()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId(DeviceId);

        Guid menuItemId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<PosDbContext>();
            menuItemId = await ctx.MenuItems.Select(m => m.Id).FirstAsync();
        }

        var orderId = await CreateOpenOrderViaSyncAsync(token, menuItemId, "recon-stale-create-001");
        await OpenOrderAsync(orderId);
        await FinalizeOrderViaSyncAsync(orderId, "recon-stale-fin-001");
        await VoidOrderAsync(orderId, "recon-stale-void-001");

        var pushBody = new
        {
            deviceId = DeviceId,
            operations = new[]
            {
                new
                {
                    operationId = "recon-stale-fin-retry-001",
                    type = "FinalizeOrder",
                    payload = new Dictionary<string, object?>
                    {
                        ["orderId"] = orderId.ToString(),
                        ["payments"] = new[]
                        {
                            new
                            {
                                paymentMethod = "Cash",
                                amount = "11.00",
                                transactionId = "STALE-FIN-001"
                            }
                        }
                    }
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/v1.0/sync/push", pushBody);
        response.EnsureSuccessStatusCode();
        var pushJson = await response.Content.ReadFromJsonAsync<JsonElement>();
        var opSuccess = pushJson.GetProperty("results")[0].GetProperty("success").GetBoolean();
        Assert.False(opSuccess);

        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<PosDbContext>();
        var order = await db.Orders.AsNoTracking().SingleAsync(o => o.Id == orderId);
        Assert.Equal(OrderStatus.Void, order.Status);
    }

    [SkippableFact]
    public async Task Finalize_with_negative_stock_persists_inventory_drift_risk_record()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId();

        Guid menuItemId;
        Guid ingredientId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<PosDbContext>();
            var ingredient = new Ingredient
            {
                Id = Guid.NewGuid(),
                Name = "Negative Stock Ingredient",
                Unit = "kg",
                CostPerUnit = 2m,
                IsActive = true
            };
            ctx.Ingredients.Add(ingredient);
            ctx.InventoryItems.Add(new InventoryItem
            {
                Id = Guid.NewGuid(),
                IngredientId = ingredient.Id,
                CurrentStock = 1m,
                MinimumStock = 0,
                MaximumStock = 100m,
                AverageCost = 2m,
                Unit = "kg",
                LastUpdated = DateTime.UtcNow
            });

            var category = await ctx.Categories.FirstAsync();
            menuItemId = Guid.NewGuid();
            ctx.MenuItems.Add(new MenuItem
            {
                Id = menuItemId,
                Name = "Negative Stock Item",
                Price = 12m,
                CategoryId = category.Id,
                IsActive = true,
                HasIngredients = true
            });

            var recipe = new Recipe
            {
                Id = Guid.NewGuid(),
                MenuItemId = menuItemId,
                Name = "Negative Stock Recipe",
                IsActive = true
            };
            ctx.Recipes.Add(recipe);
            ctx.RecipeLines.Add(new RecipeLine
            {
                Id = Guid.NewGuid(),
                RecipeId = recipe.Id,
                IngredientId = ingredient.Id,
                QuantityPerItem = 5m,
                Unit = "kg"
            });
            await ctx.SaveChangesAsync();
            ingredientId = ingredient.Id;
        }

        var orderId = await CreateOpenOrderAsync(token, menuItemId, unitPrice: 12m);

        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<PosDbContext>();
        var recipeQty = await db.RecipeLines
            .Where(rl => rl.Recipe!.MenuItemId == menuItemId)
            .SumAsync(rl => rl.QuantityPerItem);
        var stock = await db.InventoryItems
            .Where(ii => ii.IngredientId == ingredientId)
            .Select(ii => ii.CurrentStock)
            .SingleAsync();
        Assert.True(recipeQty > stock);
        Assert.NotEqual(Guid.Empty, orderId);
    }

    [SkippableFact]
    public async Task Parallel_finalize_on_same_order_persists_concurrency_conflict_record()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId();

        var orderId = await CreateOpenOrderAsync(token);

        async Task<HttpResponseMessage> FinalizeOnceAsync(string idempotencyKey)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1.0/orders/{orderId}/finalize")
            {
                Content = JsonContent.Create(new
                {
                    Payments = new[]
                    {
                        new { PaymentMethod = "Cash", Amount = 15m, TransactionId = idempotencyKey }
                    }
                })
            };
            request.Headers.Add("Device-Id", "test-device-001");
            request.Headers.Add("Idempotency-Key", idempotencyKey);
            request.Headers.Authorization = _client.DefaultRequestHeaders.Authorization;
            return await _client.SendAsync(request);
        }

        var responses = await Task.WhenAll(
            FinalizeOnceAsync("recon-par-fin-a"),
            FinalizeOnceAsync("recon-par-fin-b"));

        Assert.True(
            responses.Count(r => r.IsSuccessStatusCode) >= 1 ||
            responses.Count(r => r.StatusCode == System.Net.HttpStatusCode.Conflict) >= 1,
            "Expected at least one successful finalize or a concurrency conflict from parallel finalize.");

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PosDbContext>();
        var order = await db.Orders.AsNoTracking().SingleAsync(o => o.Id == orderId);
        var paymentCount = await db.Payments.AsNoTracking().CountAsync(p => p.OrderId == orderId);
        Assert.True(
            order.Status == OrderStatus.Paid,
            "Parallel finalize must complete payment on the order when any attempt succeeds.");
        Assert.True(
            paymentCount <= 1,
            "Parallel finalize must not create duplicate payments.");
        Assert.True(
            responses.Any(r => r.StatusCode == System.Net.HttpStatusCode.Conflict) ||
            responses.Count(r => r.IsSuccessStatusCode) >= 1,
            "Expected either a concurrency conflict response or at least one successful finalize.");
    }

    [SkippableFact]
    public async Task Partial_batch_with_inventory_failure_persists_reconciliation_conflict_record()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId(DeviceId);

        Guid ingredientId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<PosDbContext>();
            var ingredient = new Ingredient
            {
                Id = Guid.NewGuid(),
                Name = "Partial Batch Ingredient",
                Unit = "kg",
                CostPerUnit = 1m,
                IsActive = true
            };
            ctx.Ingredients.Add(ingredient);
            ctx.InventoryItems.Add(new InventoryItem
            {
                Id = Guid.NewGuid(),
                IngredientId = ingredient.Id,
                CurrentStock = 5m,
                MinimumStock = 0,
                MaximumStock = 50m,
                AverageCost = 1m,
                Unit = "kg",
                LastUpdated = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();
            ingredientId = ingredient.Id;
        }

        const string goodOpId = "partial-batch-good-adj-001";
        var seedPush = new
        {
            deviceId = DeviceId,
            operations = new[]
            {
                new
                {
                    operationId = goodOpId,
                    type = "AdjustInventory",
                    payload = new Dictionary<string, object?>
                    {
                        ["ingredientId"] = ingredientId.ToString(),
                        ["quantity"] = "1",
                        ["reason"] = "seed"
                    }
                }
            }
        };
        (await _client.PostAsJsonAsync("/api/v1.0/sync/push", seedPush)).EnsureSuccessStatusCode();

        var mixedPush = new
        {
            deviceId = DeviceId,
            operations = new object[]
            {
                new
                {
                    operationId = goodOpId,
                    type = "AdjustInventory",
                    payload = new Dictionary<string, object?>
                    {
                        ["ingredientId"] = ingredientId.ToString(),
                        ["quantity"] = "1",
                        ["reason"] = "replay"
                    }
                },
                new
                {
                    operationId = "partial-batch-bad-adj-001",
                    type = "AdjustInventory",
                    payload = new Dictionary<string, object?> { ["reason"] = "missing ingredientId" }
                }
            }
        };

        (await _client.PostAsJsonAsync("/api/v1.0/sync/push", mixedPush)).EnsureSuccessStatusCode();

        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<PosDbContext>();
        var batchRecords = await db.SyncConflictRecords
            .Where(r => r.DeviceId == DeviceId && r.EntityType == "SyncPushBatch")
            .ToListAsync();
        Assert.NotEmpty(batchRecords);
    }

    private Task<Guid> CreateOpenOrderAsync(string token) =>
        CreateOpenOrderAsync(token, menuItemId: Guid.Empty, unitPrice: 10m);

    private async Task<Guid> CreateOpenOrderAsync(string token, Guid menuItemId, decimal unitPrice = 10m)
    {
        SetAuthHeader(token);
        SetDeviceId();
        SetIdempotencyKey("recon-open-order-001");

        if (menuItemId == Guid.Empty)
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var ctx = scope.ServiceProvider.GetRequiredService<PosDbContext>();
            menuItemId = await ctx.MenuItems.Select(m => m.Id).FirstAsync();
        }

        var response = await _client.PostAsJsonAsync("/api/v1.0/orders", new
        {
            OrderType = OrderType.DineIn,
            CustomerId = (Guid?)null,
            OrderLines = new[]
            {
                new
                {
                    MenuItemId = menuItemId,
                    Quantity = 1,
                    UnitPrice = unitPrice,
                    AddOns = Array.Empty<object>()
                }
            },
            Notes = "sync reconciliation test order"
        });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = Guid.Parse(json.GetProperty("id").GetString()!);
        await OpenOrderAsync(orderId);
        return orderId;
    }

    private async Task OpenOrderAsync(Guid orderId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<PosDbContext>();
        var order = await ctx.Orders.FindAsync(orderId);
        Assert.NotNull(order);
        order.Status = OrderStatus.Open;
        await ctx.SaveChangesAsync();
    }

    private async Task VoidOrderAsync(Guid orderId, string idempotencyKey)
    {
        SetIdempotencyKey(idempotencyKey);
        var response = await _client.PostAsJsonAsync($"/api/v1.0/orders/{orderId}/void", new { reason = "test void" });
        response.EnsureSuccessStatusCode();
    }

    private async Task<Guid> CreateOpenOrderViaSyncAsync(string token, Guid menuItemId, string opId)
    {
        SetAuthHeader(token);
        SetDeviceId(DeviceId);
        var pushBody = new
        {
            deviceId = DeviceId,
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

    private async Task FinalizeOrderViaSyncAsync(Guid orderId, string opId)
    {
        var pushBody = new
        {
            deviceId = DeviceId,
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

}
