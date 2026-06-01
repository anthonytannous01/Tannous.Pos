using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Integration;

public class SyncDurableReplayIntegrationTests : IntegrationTestBase
{
    public SyncDurableReplayIntegrationTests(IntegrationPostgresFixture postgresFixture) : base(postgresFixture)
    {
    }

    protected override async Task SeedTestDataAsync(PosDbContext context)
    {
        context.Devices.Add(new Device
        {
            Id = Guid.NewGuid(),
            DeviceId = "replay-device-001",
            Name = "Replay Device",
            DeviceType = "Terminal",
            IsActive = true
        });

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Sync Test Category",
            IsActive = true,
            DisplayOrder = 1
        };
        context.Categories.Add(category);

        context.MenuItems.Add(new MenuItem
        {
            Id = Guid.NewGuid(),
            Name = "Sync Test Item",
            Price = 9.99m,
            CategoryId = category.Id,
            IsActive = true
        });

        await context.SaveChangesAsync();
    }

    [SkippableFact]
    public async Task CreateOrder_duplicate_push_persists_single_real_order_and_returns_same_server_id()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId("replay-device-001");

        Guid menuItemId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<PosDbContext>();
            menuItemId = await ctx.MenuItems.Select(x => x.Id).FirstAsync();
        }

        const string opId = "sync-create-replay-001";
        var pushBody = new
        {
            deviceId = "replay-device-001",
            operations = new[]
            {
                new
                {
                    operationId = opId,
                    type = "CreateOrder",
                    payload = new Dictionary<string, object?>
                    {
                        ["orderType"] = "DineIn",
                        ["notes"] = "sync-create-order",
                        ["orderLines"] = new[]
                        {
                            new
                            {
                                menuItemId = menuItemId.ToString(),
                                quantity = "2",
                                unitPrice = "9.99"
                            }
                        }
                    }
                }
            }
        };

        var r1 = await _client.PostAsJsonAsync("/api/v1.0/sync/push", pushBody);
        var r2 = await _client.PostAsJsonAsync("/api/v1.0/sync/push", pushBody);
        r1.EnsureSuccessStatusCode();
        r2.EnsureSuccessStatusCode();

        var j1 = await r1.Content.ReadFromJsonAsync<JsonElement>();
        var j2 = await r2.Content.ReadFromJsonAsync<JsonElement>();
        var sid1 = j1.GetProperty("results")[0].GetProperty("serverId").GetString();
        var sid2 = j2.GetProperty("results")[0].GetProperty("serverId").GetString();
        Assert.False(string.IsNullOrWhiteSpace(sid1));
        Assert.Equal(sid1, sid2);

        await using var scope2 = _factory.Services.CreateAsyncScope();
        var db = scope2.ServiceProvider.GetRequiredService<PosDbContext>();
        var createdOrderId = Guid.Parse(sid1!);
        var order = await db.Orders.Include(o => o.OrderLines).FirstOrDefaultAsync(o => o.Id == createdOrderId);
        Assert.NotNull(order);
        Assert.Single(order!.OrderLines);

        var receipts = await db.SyncOperationReceipts.Where(r => r.OperationId == opId).ToListAsync();
        Assert.Single(receipts);
    }

    [SkippableFact]
    public async Task FinalizeOrder_duplicate_push_executes_real_finalize_and_avoids_duplicate_payments()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId("replay-device-001");
        SetIdempotencyKey("sync-finalize-prep-create");

        Guid menuItemId;
        await using (var setupScope = _factory.Services.CreateAsyncScope())
        {
            var setupDb = setupScope.ServiceProvider.GetRequiredService<PosDbContext>();
            menuItemId = await setupDb.MenuItems.Select(x => x.Id).FirstAsync();
        }

        var createOrderResponse = await _client.PostAsJsonAsync(
            "/api/v1.0/orders",
            BuildCreateOrderPayload(menuItemId, 10.00m, notes: "sync-finalize-order"));
        var orderId = await ReadCreatedOrderIdAsync(createOrderResponse);
        await TransitionOrderToOpenAsync(orderId);
        var paymentTotal = TotalWithLegacyTax(10.00m).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

        const string opId = "sync-finalize-replay-001";
        var pushBody = new
        {
            deviceId = "replay-device-001",
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
                                amount = paymentTotal,
                                transactionId = "SYNC-FIN-001"
                            }
                        }
                    }
                }
            }
        };

        var r1 = await _client.PostAsJsonAsync("/api/v1.0/sync/push", pushBody);
        var r2 = await _client.PostAsJsonAsync("/api/v1.0/sync/push", pushBody);
        await AssertSyncOperationSuccessAsync(r1);
        await AssertSyncOperationSuccessAsync(r2);

        await using var scope3 = _factory.Services.CreateAsyncScope();
        var db = scope3.ServiceProvider.GetRequiredService<PosDbContext>();
        var order = await db.Orders.FirstAsync(o => o.Id == orderId);
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.False(string.IsNullOrWhiteSpace(order.ReceiptNumber));

        var paymentCount = await db.Payments.CountAsync(p => p.OrderId == orderId);
        Assert.Equal(1, paymentCount);

        var receiptRows = await db.SyncOperationReceipts.Where(r => r.OperationId == opId).ToListAsync();
        Assert.Single(receiptRows);
    }

    [SkippableFact]
    public async Task FinalizeOrder_parallel_replay_same_operation_id_single_payment_and_single_receipt()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId("replay-device-001");
        SetIdempotencyKey("sync-finalize-parallel-prep");

        Guid menuItemId;
        await using (var setupScope = _factory.Services.CreateAsyncScope())
        {
            var setupDb = setupScope.ServiceProvider.GetRequiredService<PosDbContext>();
            menuItemId = await setupDb.MenuItems.Select(x => x.Id).FirstAsync();
        }

        var createOrderResponse = await _client.PostAsJsonAsync(
            "/api/v1.0/orders",
            BuildCreateOrderPayload(menuItemId, 10.00m, notes: "sync-finalize-parallel"));
        var orderId = await ReadCreatedOrderIdAsync(createOrderResponse);
        await TransitionOrderToOpenAsync(orderId);
        var paymentTotal = TotalWithLegacyTax(10.00m).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

        const string opId = "sync-finalize-replay-parallel-001";
        var pushBody = new
        {
            deviceId = "replay-device-001",
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
                                amount = paymentTotal,
                                transactionId = "SYNC-FIN-PAR-001"
                            }
                        }
                    }
                }
            }
        };

        var r1 = await _client.PostAsJsonAsync("/api/v1.0/sync/push", pushBody);
        await AssertSyncOperationSuccessAsync(r1);
        var r2 = await _client.PostAsJsonAsync("/api/v1.0/sync/push", pushBody);
        await AssertSyncOperationSuccessAsync(r2);

        await using var scopeVerify = _factory.Services.CreateAsyncScope();
        var db = scopeVerify.ServiceProvider.GetRequiredService<PosDbContext>();
        var paymentCount = await db.Payments.CountAsync(p => p.OrderId == orderId);
        Assert.Equal(1, paymentCount);
        var receiptCount = await db.SyncOperationReceipts.CountAsync(r => r.OperationId == opId && r.DeviceId == "replay-device-001");
        Assert.Equal(1, receiptCount);
    }

    [SkippableFact]
    public async Task CashDrop_duplicate_push_does_not_duplicate_cash_events()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId("replay-device-001");

        Guid userId;
        await using (var userScope = _factory.Services.CreateAsyncScope())
        {
            var userDb = userScope.ServiceProvider.GetRequiredService<PosDbContext>();
            userId = await userDb.Users.Where(u => u.Username == "owner").Select(u => u.Id).FirstAsync();
        }

        Guid shiftId;
        await using (var shiftScope = _factory.Services.CreateAsyncScope())
        {
            var shiftDb = shiftScope.ServiceProvider.GetRequiredService<PosDbContext>();
            var shift = new Shift
            {
                Id = Guid.NewGuid(),
                ShiftNumber = "SYNC-SHIFT-001",
                StartTime = DateTime.UtcNow.AddMinutes(-10),
                OpeningBalance = 100m,
                Status = ShiftStatus.Open,
                UserId = userId
            };
            shiftDb.Shifts.Add(shift);
            await shiftDb.SaveChangesAsync();
            shiftId = shift.Id;
        }

        const string opId = "sync-cashdrop-replay-001";
        var pushBody = new
        {
            deviceId = "replay-device-001",
            operations = new[]
            {
                new
                {
                    operationId = opId,
                    type = "CashDrop",
                    payload = new Dictionary<string, object?>
                    {
                        ["shiftId"] = shiftId.ToString(),
                        ["amount"] = "15.00",
                        ["reason"] = "sync-drop"
                    }
                }
            }
        };

        var r1 = await _client.PostAsJsonAsync("/api/v1.0/sync/push", pushBody);
        var r2 = await _client.PostAsJsonAsync("/api/v1.0/sync/push", pushBody);
        await AssertSyncOperationSuccessAsync(r1);
        await AssertSyncOperationSuccessAsync(r2);

        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<PosDbContext>();
        var events = await db.CashDrawerEvents.Where(e => e.ShiftId == shiftId && e.EventType == "Drop").ToListAsync();
        Assert.Single(events);

        var receipts = await db.SyncOperationReceipts.Where(r => r.OperationId == opId).ToListAsync();
        Assert.Single(receipts);
    }

    [SkippableFact]
    public async Task AdjustInventory_duplicate_push_same_operation_id_single_movement_and_single_receipt()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId("replay-device-001");

        Guid ingredientId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<PosDbContext>();
            var ingredient = new Ingredient
            {
                Id = Guid.NewGuid(),
                Name = "Replay Ingredient Adj",
                Unit = "kg",
                CostPerUnit = 3.00m,
                IsActive = true
            };
            ctx.Ingredients.Add(ingredient);
            ctx.InventoryItems.Add(new InventoryItem
            {
                Id = Guid.NewGuid(),
                IngredientId = ingredient.Id,
                CurrentStock = 50.0m,
                MinimumStock = 1.0m,
                MaximumStock = 100.0m,
                AverageCost = 3.00m,
                Unit = "kg",
                LastUpdated = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();
            ingredientId = ingredient.Id;
        }

        const string opId = "sync-adj-replay-002";
        var pushBody = new
        {
            deviceId = "replay-device-001",
            operations = new[]
            {
                new
                {
                    operationId = opId,
                    type = "AdjustInventory",
                    payload = new Dictionary<string, object?>
                    {
                        ["ingredientId"] = ingredientId.ToString(),
                        ["quantity"] = "2",
                        ["reason"] = "replay-test-adj"
                    }
                }
            }
        };

        var r1 = await _client.PostAsJsonAsync("/api/v1.0/sync/push", pushBody);
        var r2 = await _client.PostAsJsonAsync("/api/v1.0/sync/push", pushBody);
        r1.EnsureSuccessStatusCode();
        r2.EnsureSuccessStatusCode();

        var j1 = await r1.Content.ReadFromJsonAsync<JsonElement>();
        var j2 = await r2.Content.ReadFromJsonAsync<JsonElement>();
        var sid1 = j1.GetProperty("results")[0].GetProperty("serverId").GetString();
        var sid2 = j2.GetProperty("results")[0].GetProperty("serverId").GetString();
        Assert.Equal(sid1, sid2);

        await using var scope2 = _factory.Services.CreateAsyncScope();
        var db = scope2.ServiceProvider.GetRequiredService<PosDbContext>();
        var movements = await db.InventoryMovements
            .Where(m => m.IngredientId == ingredientId && m.MovementType == InventoryMovementType.Adjustment)
            .ToListAsync();
        Assert.Single(movements);
        var ii = await db.InventoryItems.FirstAsync(x => x.IngredientId == ingredientId);
        Assert.Equal(52.0m, ii.CurrentStock);

        var receipts = await db.SyncOperationReceipts.Where(x => x.OperationId == opId).ToListAsync();
        Assert.Single(receipts);
    }

    [SkippableFact]
    public async Task RecordWastage_duplicate_push_single_wastage_row_stable_server_id()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId("replay-device-001");

        Guid ingredientId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<PosDbContext>();
            var ingredient = new Ingredient
            {
                Id = Guid.NewGuid(),
                Name = "Wastage Ingredient Replay",
                Unit = "kg",
                CostPerUnit = 2.00m,
                IsActive = true
            };
            ctx.Ingredients.Add(ingredient);
            ctx.InventoryItems.Add(new InventoryItem
            {
                Id = Guid.NewGuid(),
                IngredientId = ingredient.Id,
                CurrentStock = 20.0m,
                MinimumStock = 1.0m,
                MaximumStock = 50.0m,
                AverageCost = 2.00m,
                Unit = "kg",
                LastUpdated = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();
            ingredientId = ingredient.Id;
        }

        const string opId = "sync-waste-replay-002";
        var pushBody = new
        {
            deviceId = "replay-device-001",
            operations = new[]
            {
                new
                {
                    operationId = opId,
                    type = "RecordWastage",
                    payload = new Dictionary<string, object?>
                    {
                        ["ingredientId"] = ingredientId.ToString(),
                        ["quantity"] = "1",
                        ["reason"] = "replay-test-waste"
                    }
                }
            }
        };

        var r1 = await _client.PostAsJsonAsync("/api/v1.0/sync/push", pushBody);
        var r2 = await _client.PostAsJsonAsync("/api/v1.0/sync/push", pushBody);
        r1.EnsureSuccessStatusCode();
        r2.EnsureSuccessStatusCode();

        var j1 = await r1.Content.ReadFromJsonAsync<JsonElement>();
        var j2 = await r2.Content.ReadFromJsonAsync<JsonElement>();
        var sid1 = j1.GetProperty("results")[0].GetProperty("serverId").GetString();
        var sid2 = j2.GetProperty("results")[0].GetProperty("serverId").GetString();
        Assert.Equal(sid1, sid2);

        await using var scope2 = _factory.Services.CreateAsyncScope();
        var db = scope2.ServiceProvider.GetRequiredService<PosDbContext>();
        var iiId = await db.InventoryItems.Where(ii => ii.IngredientId == ingredientId).Select(ii => ii.Id).SingleAsync();
        var wastageRows = await db.WastageRecords.Where(w => w.InventoryItemId == iiId).ToListAsync();
        Assert.Single(wastageRows);
        var movements = await db.InventoryMovements
            .Where(m => m.IngredientId == ingredientId && m.MovementType == InventoryMovementType.Wastage)
            .ToListAsync();
        Assert.Single(movements);

        var receipts = await db.SyncOperationReceipts.Where(r => r.OperationId == opId).ToListAsync();
        Assert.Single(receipts);
    }

    [SkippableFact]
    public async Task AdjustInventory_parallel_replay_same_operation_id_single_movement()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId("replay-device-001");

        Guid ingredientId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<PosDbContext>();
            var ingredient = new Ingredient
            {
                Id = Guid.NewGuid(),
                Name = "Replay Ingredient Par",
                Unit = "kg",
                CostPerUnit = 4.00m,
                IsActive = true
            };
            ctx.Ingredients.Add(ingredient);
            ctx.InventoryItems.Add(new InventoryItem
            {
                Id = Guid.NewGuid(),
                IngredientId = ingredient.Id,
                CurrentStock = 100.0m,
                MinimumStock = 1.0m,
                MaximumStock = 200.0m,
                AverageCost = 4.00m,
                Unit = "kg",
                LastUpdated = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();
            ingredientId = ingredient.Id;
        }

        const string opId = "sync-adj-replay-parallel-002";
        var pushBody = new
        {
            deviceId = "replay-device-001",
            operations = new[]
            {
                new
                {
                    operationId = opId,
                    type = "AdjustInventory",
                    payload = new Dictionary<string, object?>
                    {
                        ["ingredientId"] = ingredientId.ToString(),
                        ["quantity"] = "3",
                        ["reason"] = "replay-parallel-adj"
                    }
                }
            }
        };

        var t1 = _client.PostAsJsonAsync("/api/v1.0/sync/push", pushBody);
        var t2 = _client.PostAsJsonAsync("/api/v1.0/sync/push", pushBody);
        var responses = await Task.WhenAll(t1, t2);
        foreach (var r in responses)
        {
            r.EnsureSuccessStatusCode();
        }

        await using var verify = _factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<PosDbContext>();
        var movements = await db.InventoryMovements
            .Where(m => m.IngredientId == ingredientId && m.MovementType == InventoryMovementType.Adjustment)
            .ToListAsync();
        Assert.Single(movements);
        Assert.Equal(1, await db.SyncOperationReceipts.CountAsync(r => r.OperationId == opId && r.DeviceId == "replay-device-001"));
    }

    [SkippableFact]
    public async Task OpenShift_duplicate_push_single_receipt_stable_server_id()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId("replay-device-001");

        const string opId = "sync-open-shift-replay-001";
        var pushBody = new
        {
            deviceId = "replay-device-001",
            operations = new[]
            {
                new
                {
                    operationId = opId,
                    type = "OpenShift",
                    payload = new Dictionary<string, object?>()
                }
            }
        };

        var r1 = await _client.PostAsJsonAsync("/api/v1.0/sync/push", pushBody);
        var r2 = await _client.PostAsJsonAsync("/api/v1.0/sync/push", pushBody);
        r1.EnsureSuccessStatusCode();
        r2.EnsureSuccessStatusCode();

        var j1 = await r1.Content.ReadFromJsonAsync<JsonElement>();
        var j2 = await r2.Content.ReadFromJsonAsync<JsonElement>();
        var sid1 = j1.GetProperty("results")[0].GetProperty("serverId").GetString();
        var sid2 = j2.GetProperty("results")[0].GetProperty("serverId").GetString();
        Assert.Equal(sid1, sid2);
        Assert.True(j1.GetProperty("results")[0].GetProperty("success").GetBoolean());
        Assert.True(j2.GetProperty("results")[0].GetProperty("success").GetBoolean());

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PosDbContext>();
        var receipts = await db.SyncOperationReceipts.Where(r => r.OperationId == opId).ToListAsync();
        Assert.Single(receipts);
        Assert.Equal("OpenShift", receipts[0].OperationType);
    }

    [SkippableFact]
    public async Task CreateCustomer_duplicate_push_single_receipt_stable_server_id()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId("replay-device-001");

        const string opId = "sync-create-customer-replay-001";
        var pushBody = new
        {
            deviceId = "replay-device-001",
            operations = new[]
            {
                new
                {
                    operationId = opId,
                    type = "CreateCustomer",
                    payload = new Dictionary<string, object?>()
                }
            }
        };

        var r1 = await _client.PostAsJsonAsync("/api/v1.0/sync/push", pushBody);
        var r2 = await _client.PostAsJsonAsync("/api/v1.0/sync/push", pushBody);
        r1.EnsureSuccessStatusCode();
        r2.EnsureSuccessStatusCode();

        var j1 = await r1.Content.ReadFromJsonAsync<JsonElement>();
        var j2 = await r2.Content.ReadFromJsonAsync<JsonElement>();
        var sid1 = j1.GetProperty("results")[0].GetProperty("serverId").GetString();
        var sid2 = j2.GetProperty("results")[0].GetProperty("serverId").GetString();
        Assert.Equal(sid1, sid2);
        Assert.Equal(opId, sid1);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PosDbContext>();
        var receipts = await db.SyncOperationReceipts.Where(r => r.OperationId == opId).ToListAsync();
        Assert.Single(receipts);
        Assert.Equal("CreateCustomer", receipts[0].OperationType);
    }

    [SkippableFact]
    public async Task OpenShift_parallel_replay_same_operation_id_single_receipt()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId("replay-device-001");

        const string opId = "sync-open-shift-replay-parallel-001";
        var pushBody = new
        {
            deviceId = "replay-device-001",
            operations = new[]
            {
                new
                {
                    operationId = opId,
                    type = "OpenShift",
                    payload = new Dictionary<string, object?>()
                }
            }
        };

        var t1 = _client.PostAsJsonAsync("/api/v1.0/sync/push", pushBody);
        var t2 = _client.PostAsJsonAsync("/api/v1.0/sync/push", pushBody);
        var responses = await Task.WhenAll(t1, t2);
        foreach (var r in responses)
            r.EnsureSuccessStatusCode();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PosDbContext>();
        Assert.Equal(1, await db.SyncOperationReceipts.CountAsync(r => r.OperationId == opId && r.DeviceId == "replay-device-001"));
    }

    [SkippableFact]
    public async Task CreateCustomer_parallel_replay_same_operation_id_single_receipt()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId("replay-device-001");

        const string opId = "sync-create-customer-replay-parallel-001";
        var pushBody = new
        {
            deviceId = "replay-device-001",
            operations = new[]
            {
                new
                {
                    operationId = opId,
                    type = "CreateCustomer",
                    payload = new Dictionary<string, object?>()
                }
            }
        };

        var t1 = _client.PostAsJsonAsync("/api/v1.0/sync/push", pushBody);
        var t2 = _client.PostAsJsonAsync("/api/v1.0/sync/push", pushBody);
        var responses = await Task.WhenAll(t1, t2);
        foreach (var r in responses)
            r.EnsureSuccessStatusCode();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PosDbContext>();
        Assert.Equal(1, await db.SyncOperationReceipts.CountAsync(r => r.OperationId == opId && r.DeviceId == "replay-device-001"));
    }
}
