using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Orders;
using Tannous.Pos.Application.Sync;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Integration;

/// <summary>
/// High-contention integration scenarios for durable replay, money-path races, audit survivability, and forensic export caps.
/// </summary>
public class ReplayConcurrencyStressIntegrationTests : IntegrationTestBase
{
    private const string StressDeviceId = "stress-replay-device-001";
    private const string ExportBase = "/api/v1.0/internal/operational-audit/export";
    private const int StormCount = 20;

    public ReplayConcurrencyStressIntegrationTests(IntegrationPostgresFixture postgresFixture) : base(postgresFixture)
    {
    }

    protected override async Task SeedTestDataAsync(PosDbContext context)
    {
        context.Devices.Add(new Device
        {
            Id = Guid.NewGuid(),
            DeviceId = StressDeviceId,
            Name = "Stress Replay Device",
            DeviceType = "Terminal",
            IsActive = true
        });

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Stress Sync Category",
            IsActive = true,
            DisplayOrder = 1
        };
        context.Categories.Add(category);

        context.MenuItems.Add(new MenuItem
        {
            Id = Guid.NewGuid(),
            Name = "Stress Sync Item",
            Price = 9.99m,
            CategoryId = category.Id,
            IsActive = true
        });

        await context.SaveChangesAsync();
    }

    private static async Task<HttpResponseMessage[]> StormSyncPushAsync(HttpClient client, object body, int count)
    {
        var tasks = new Task<HttpResponseMessage>[count];
        for (var i = 0; i < count; i++)
            tasks[i] = client.PostAsJsonAsync("/api/v1.0/sync/push", body);
        return await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Under Serializable durable-replay locking, not every concurrent push returns HTTP 200 in the same window.
    /// Asserts stable replay contract: at least one success, matching serverIds on successful ops, no 5xx.
    /// </summary>
    private static async Task<string?> AssertSyncReplayStormContractAsync(
        HttpResponseMessage[] responses,
        int minSuccessfulOps = 1)
    {
        Assert.DoesNotContain(responses, r => (int)r.StatusCode >= 500);

        var successServerIds = new List<string>();
        var successCount = 0;
        foreach (var r in responses.Where(r => r.IsSuccessStatusCode))
        {
            var json = await r.Content.ReadFromJsonAsync<JsonElement>();
            if (!json.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
                continue;

            var result = results[0];
            if (result.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
            {
                successCount++;
                if (result.TryGetProperty("serverId", out var sidProp))
                {
                    var sid = sidProp.GetString();
                    if (!string.IsNullOrWhiteSpace(sid))
                        successServerIds.Add(sid);
                }
            }
        }

        Assert.True(
            successCount >= minSuccessfulOps,
            $"Expected at least {minSuccessfulOps} successful sync operations, got {successCount}.");

        if (successServerIds.Count > 0)
            Assert.All(successServerIds, id => Assert.Equal(successServerIds[0], id));

        return successServerIds.Count > 0 ? successServerIds[0] : null;
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

    [SkippableFact]
    public async Task CreateOrder_replay_storm_single_order_single_receipt()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId(StressDeviceId);

        Guid menuItemId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<PosDbContext>();
            menuItemId = await ctx.MenuItems.Select(x => x.Id).FirstAsync();
        }

        const string opId = "stress-sync-create-001";
        var pushBody = new
        {
            deviceId = StressDeviceId,
            operations = new object[]
            {
                new
                {
                    operationId = opId,
                    type = "CreateOrder",
                    payload = new Dictionary<string, object?>
                    {
                        ["orderType"] = "DineIn",
                        ["notes"] = "stress-create",
                        ["orderLines"] = new[]
                        {
                            new { menuItemId = menuItemId.ToString(), quantity = "1", unitPrice = "9.99" }
                        }
                    }
                }
            }
        };

        var responses = await StormSyncPushAsync(_client, pushBody, StormCount);
        var sid = await AssertSyncReplayStormContractAsync(responses);

        await using var verify = _factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<PosDbContext>();
        Assert.Equal(1, await db.SyncOperationReceipts.CountAsync(r => r.DeviceId == StressDeviceId && r.OperationId == opId));
        Assert.False(string.IsNullOrWhiteSpace(sid));
        Assert.Equal(1, await db.Orders.CountAsync(o => o.Id == Guid.Parse(sid!)));
    }

    [SkippableFact]
    public async Task FinalizeOrder_replay_storm_single_payment_single_receipt()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId(StressDeviceId);
        SetIdempotencyKey("stress-fin-prep");

        Guid menuItemId;
        await using (var setupScope = _factory.Services.CreateAsyncScope())
        {
            var setupDb = setupScope.ServiceProvider.GetRequiredService<PosDbContext>();
            menuItemId = await setupDb.MenuItems.Select(x => x.Id).FirstAsync();
        }

        var createOrderResponse = await _client.PostAsJsonAsync(
            "/api/v1.0/orders",
            BuildCreateOrderPayload(menuItemId, 10.00m, notes: "stress-finalize"));
        var orderId = await ReadCreatedOrderIdAsync(createOrderResponse);
        await TransitionOrderToOpenAsync(orderId);
        var paymentTotal = TotalWithLegacyTax(10.00m).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

        const string opId = "stress-sync-finalize-001";
        var pushBody = new
        {
            deviceId = StressDeviceId,
            operations = new object[]
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
                                transactionId = "STRESS-FIN-001"
                            }
                        }
                    }
                }
            }
        };

        var responses = await StormSyncPushAsync(_client, pushBody, StormCount);
        await AssertSyncReplayStormContractAsync(responses);

        await using var verify = _factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<PosDbContext>();
        Assert.Equal(1, await db.Payments.CountAsync(p => p.OrderId == orderId));
        Assert.Equal(1, await db.SyncOperationReceipts.CountAsync(r => r.DeviceId == StressDeviceId && r.OperationId == opId));
        Assert.Equal(OrderStatus.Paid, await db.Orders.Where(o => o.Id == orderId).Select(o => o.Status).SingleAsync());
    }

    [SkippableFact]
    public async Task AdjustInventory_replay_storm_single_movement_single_receipt()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId(StressDeviceId);

        Guid ingredientId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<PosDbContext>();
            var ingredient = new Ingredient
            {
                Id = Guid.NewGuid(),
                Name = "Stress Adj Ingredient",
                Unit = "kg",
                CostPerUnit = 2m,
                IsActive = true
            };
            ctx.Ingredients.Add(ingredient);
            ctx.InventoryItems.Add(new InventoryItem
            {
                Id = Guid.NewGuid(),
                IngredientId = ingredient.Id,
                CurrentStock = 40m,
                MinimumStock = 0m,
                MaximumStock = 100m,
                AverageCost = 2m,
                Unit = "kg",
                LastUpdated = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();
            ingredientId = ingredient.Id;
        }

        const string opId = "stress-sync-adj-001";
        var pushBody = new
        {
            deviceId = StressDeviceId,
            operations = new object[]
            {
                new
                {
                    operationId = opId,
                    type = "AdjustInventory",
                    payload = new Dictionary<string, object?>
                    {
                        ["ingredientId"] = ingredientId.ToString(),
                        ["quantity"] = "3",
                        ["reason"] = "stress-adj"
                    }
                }
            }
        };

        var responses = await StormSyncPushAsync(_client, pushBody, StormCount);
        await AssertSyncReplayStormContractAsync(responses);

        await using var verify = _factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<PosDbContext>();
        Assert.Equal(1, await db.SyncOperationReceipts.CountAsync(r => r.DeviceId == StressDeviceId && r.OperationId == opId));
        Assert.Equal(
            1,
            await db.InventoryMovements.CountAsync(m =>
                m.IngredientId == ingredientId && m.MovementType == InventoryMovementType.Adjustment));
        var stock = await db.InventoryItems.Where(ii => ii.IngredientId == ingredientId).Select(ii => ii.CurrentStock).SingleAsync();
        Assert.Equal(43m, stock);
    }

    [SkippableFact]
    public async Task RecordWastage_replay_storm_single_wastage_single_receipt()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId(StressDeviceId);

        Guid ingredientId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<PosDbContext>();
            var ingredient = new Ingredient
            {
                Id = Guid.NewGuid(),
                Name = "Stress Waste Ingredient",
                Unit = "kg",
                CostPerUnit = 1.5m,
                IsActive = true
            };
            ctx.Ingredients.Add(ingredient);
            ctx.InventoryItems.Add(new InventoryItem
            {
                Id = Guid.NewGuid(),
                IngredientId = ingredient.Id,
                CurrentStock = 25m,
                MinimumStock = 0m,
                MaximumStock = 50m,
                AverageCost = 1.5m,
                Unit = "kg",
                LastUpdated = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();
            ingredientId = ingredient.Id;
        }

        const string opId = "stress-sync-waste-001";
        var pushBody = new
        {
            deviceId = StressDeviceId,
            operations = new object[]
            {
                new
                {
                    operationId = opId,
                    type = "RecordWastage",
                    payload = new Dictionary<string, object?>
                    {
                        ["ingredientId"] = ingredientId.ToString(),
                        ["quantity"] = "2",
                        ["reason"] = "stress-waste"
                    }
                }
            }
        };

        var responses = await StormSyncPushAsync(_client, pushBody, StormCount);
        await AssertSyncReplayStormContractAsync(responses);

        await using var verify = _factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<PosDbContext>();
        Assert.Equal(1, await db.SyncOperationReceipts.CountAsync(r => r.DeviceId == StressDeviceId && r.OperationId == opId));
        Assert.Equal(1, await db.WastageRecords.CountAsync(w => w.Reason == "stress-waste"));
        Assert.Equal(
            1,
            await db.InventoryMovements.CountAsync(m =>
                m.IngredientId == ingredientId && m.MovementType == InventoryMovementType.Wastage));
        Assert.Equal(23m, await db.InventoryItems.Where(ii => ii.IngredientId == ingredientId).Select(ii => ii.CurrentStock).SingleAsync());
    }

    [SkippableFact]
    public async Task OpenShift_replay_storm_single_receipt()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId(StressDeviceId);

        const string opId = "stress-sync-openshift-001";
        var pushBody = new
        {
            deviceId = StressDeviceId,
            operations = new object[]
            {
                new { operationId = opId, type = "OpenShift", payload = new Dictionary<string, object?>() }
            }
        };

        var responses = await StormSyncPushAsync(_client, pushBody, StormCount);
        await AssertSyncReplayStormContractAsync(responses);

        await using var verify = _factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<PosDbContext>();
        Assert.Equal(1, await db.SyncOperationReceipts.CountAsync(r => r.DeviceId == StressDeviceId && r.OperationId == opId));
    }

    [SkippableFact]
    public async Task CreateCustomer_replay_storm_single_receipt()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId(StressDeviceId);

        const string opId = "stress-sync-customer-001";
        var pushBody = new
        {
            deviceId = StressDeviceId,
            operations = new object[]
            {
                new { operationId = opId, type = "CreateCustomer", payload = new Dictionary<string, object?>() }
            }
        };

        var responses = await StormSyncPushAsync(_client, pushBody, StormCount);
        await AssertSyncReplayStormContractAsync(responses);

        await using var verify = _factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<PosDbContext>();
        Assert.Equal(1, await db.SyncOperationReceipts.CountAsync(r => r.DeviceId == StressDeviceId && r.OperationId == opId));
    }

    [SkippableFact]
    public async Task Http_finalize_parallel_different_idempotency_keys_single_payment()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId();

        Guid menuItemId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<PosDbContext>();
            menuItemId = await ctx.MenuItems.Select(m => m.Id).FirstAsync();
        }

        var create = await _client.PostAsJsonAsync("/api/v1.0/orders", BuildCreateOrderPayload(menuItemId, 12.00m, notes: "stress-http-fin"));
        var orderId = await ReadCreatedOrderIdAsync(create);
        await TransitionOrderToOpenAsync(orderId);

        var pay = TotalWithLegacyTax(12.00m);
        var body = new { Payments = new[] { new { PaymentMethod = "Cash", Amount = pay, TransactionId = "STRESS-HTTP-FIN" } } };

        async Task<HttpResponseMessage> FinOnce(int i)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, $"/api/v1.0/orders/{orderId}/finalize")
            {
                Content = JsonContent.Create(body)
            };
            req.Headers.TryAddWithoutValidation("Idempotency-Key", $"stress-http-fin-{i}");
            return await _client.SendAsync(req);
        }

        var tasks = Enumerable.Range(0, 15).Select(FinOnce).ToArray();
        var responses = await Task.WhenAll(tasks);
        Assert.DoesNotContain(responses, r => (int)r.StatusCode >= 500);
        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.Equal(14, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));

        await using var verify = _factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<PosDbContext>();
        Assert.Equal(1, await db.Payments.CountAsync(p => p.OrderId == orderId));
        Assert.Equal(OrderStatus.Paid, await db.Orders.Where(o => o.Id == orderId).Select(o => o.Status).SingleAsync());
    }

    [SkippableFact]
    public async Task Http_finalize_same_idempotency_key_storm_returns_deterministic_cached_success()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId();

        Guid menuItemId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<PosDbContext>();
            menuItemId = await ctx.MenuItems.Select(m => m.Id).FirstAsync();
        }

        var create = await _client.PostAsJsonAsync("/api/v1.0/orders", BuildCreateOrderPayload(menuItemId, 14.00m, notes: "same-key-fin"));
        var orderId = await ReadCreatedOrderIdAsync(create);
        await TransitionOrderToOpenAsync(orderId);

        var pay = TotalWithLegacyTax(14.00m);
        var body = new { Payments = new[] { new { PaymentMethod = "Cash", Amount = pay, TransactionId = "STRESS-SAME-FIN" } } };
        const string sameKey = "stress-http-fin-same-key-001";

        async Task<HttpResponseMessage> FinOnce()
        {
            var req = new HttpRequestMessage(HttpMethod.Post, $"/api/v1.0/orders/{orderId}/finalize")
            {
                Content = JsonContent.Create(body)
            };
            req.Headers.TryAddWithoutValidation("Idempotency-Key", sameKey);
            return await _client.SendAsync(req);
        }

        var responses = await Task.WhenAll(Enumerable.Range(0, StormCount).Select(_ => FinOnce()));
        Assert.DoesNotContain(responses, r => (int)r.StatusCode >= 500);
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));

        await using var verify = _factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<PosDbContext>();
        Assert.Equal(1, await db.Payments.CountAsync(p => p.OrderId == orderId));
        Assert.Equal(OrderStatus.Paid, await db.Orders.Where(o => o.Id == orderId).Select(o => o.Status).SingleAsync());
        Assert.Equal(1, await db.IdempotentRequests.CountAsync(r => r.Key == sameKey && r.Endpoint == $"POST /api/orders/{orderId}/finalize"));
        Assert.Equal(0, await db.PaymentRefunds.CountAsync(r => r.OrderId == orderId));
    }

    [SkippableFact]
    public async Task Http_finalize_vs_void_parallel_terminal_state_valid_at_most_one_payment()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId();

        Guid menuItemId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<PosDbContext>();
            menuItemId = await ctx.MenuItems.Select(m => m.Id).FirstAsync();
        }

        var create = await _client.PostAsJsonAsync("/api/v1.0/orders", BuildCreateOrderPayload(menuItemId, 8.00m, notes: "stress-fin-void-race"));
        var orderId = await ReadCreatedOrderIdAsync(create);
        await TransitionOrderToOpenAsync(orderId);

        var pay = TotalWithLegacyTax(8.00m);
        var finBody = new { Payments = new[] { new { PaymentMethod = "Cash", Amount = pay, TransactionId = "STRESS-FV" } } };

        var finReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1.0/orders/{orderId}/finalize")
        {
            Content = JsonContent.Create(finBody)
        };
        finReq.Headers.TryAddWithoutValidation("Idempotency-Key", "stress-fin-vs-void-fin");

        var voidReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1.0/orders/{orderId}/void")
        {
            Content = JsonContent.Create(new { reason = "stress race void" })
        };
        voidReq.Headers.TryAddWithoutValidation("Idempotency-Key", "stress-fin-vs-void-void");

        var responses = await Task.WhenAll(_client.SendAsync(finReq), _client.SendAsync(voidReq));
        Assert.DoesNotContain(responses, r => (int)r.StatusCode >= 500);

        await using var verify = _factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<PosDbContext>();
        var status = await db.Orders.Where(o => o.Id == orderId).Select(o => o.Status).SingleAsync();
        var paymentCount = await db.Payments.CountAsync(p => p.OrderId == orderId);
        Assert.True(status is OrderStatus.Paid or OrderStatus.Void);
        Assert.InRange(paymentCount, 0, 1);
        if (status == OrderStatus.Paid)
            Assert.Equal(1, paymentCount);
    }

    [SkippableFact]
    public async Task Http_void_same_idempotency_key_storm_returns_deterministic_cached_success()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId();

        var seed = await SeedRecipeOrderForStressAsync();
        SetIdempotencyKey("stress-void-same-prep-fin");
        var create = await _client.PostAsJsonAsync(
            "/api/v1.0/orders",
            BuildCreateOrderPayload(seed.MenuItemId, 15.99m, quantity: 2, notes: "same-key-void"));
        var orderId = await ReadCreatedOrderIdAsync(create);
        await TransitionOrderToOpenAsync(orderId);

        var fin = await _client.PostAsJsonAsync(
            $"/api/v1.0/orders/{orderId}/finalize",
            new
            {
                Payments = new[]
                {
                    new { PaymentMethod = "Cash", Amount = seed.PaymentAmount, TransactionId = "STRESS-SAME-VOID-PREP" }
                }
            });
        fin.EnsureSuccessStatusCode();

        const string sameKey = "stress-http-void-same-key-001";
        async Task<HttpResponseMessage> VoidOnce()
        {
            var req = new HttpRequestMessage(HttpMethod.Post, $"/api/v1.0/orders/{orderId}/void")
            {
                Content = JsonContent.Create(new { reason = "same key void storm" })
            };
            req.Headers.TryAddWithoutValidation("Idempotency-Key", sameKey);
            return await _client.SendAsync(req);
        }

        var responses = await Task.WhenAll(Enumerable.Range(0, StormCount).Select(_ => VoidOnce()));
        Assert.DoesNotContain(responses, r => (int)r.StatusCode >= 500);
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));

        await using var verify = _factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<PosDbContext>();
        var order = await db.Orders.AsNoTracking().FirstAsync(o => o.Id == orderId);
        Assert.Equal(OrderStatus.Void, order.Status);
        Assert.Equal(1, await db.PaymentRefunds.CountAsync(r => r.OrderId == orderId));
        var voidRef = $"Order-{order.OrderNumber}-Void";
        Assert.Equal(1, await db.InventoryMovements.CountAsync(m => m.Reference == voidRef && m.MovementType == InventoryMovementType.Return));
        Assert.Equal(1, await db.IdempotentRequests.CountAsync(r => r.Key == sameKey && r.Endpoint == $"POST /api/orders/{orderId}/void"));
    }

    [SkippableFact]
    public async Task Http_void_parallel_different_idempotency_keys_no_duplicate_refunds_or_reversals()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId();

        var seed = await SeedRecipeOrderForStressAsync();
        SetIdempotencyKey("stress-void-prep-fin");
        var create = await _client.PostAsJsonAsync(
            "/api/v1.0/orders",
            BuildCreateOrderPayload(seed.MenuItemId, 15.99m, quantity: 2, notes: "stress-void-storm"));
        var orderId = await ReadCreatedOrderIdAsync(create);
        await TransitionOrderToOpenAsync(orderId);

        var fin = await _client.PostAsJsonAsync(
            $"/api/v1.0/orders/{orderId}/finalize",
            new
            {
                Payments = new[]
                {
                    new { PaymentMethod = "Cash", Amount = seed.PaymentAmount, TransactionId = "STRESS-VOID-STORM" }
                }
            });
        fin.EnsureSuccessStatusCode();

        async Task<HttpResponseMessage> VoidOnce(int i)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, $"/api/v1.0/orders/{orderId}/void")
            {
                Content = JsonContent.Create(new { reason = "storm void" })
            };
            req.Headers.TryAddWithoutValidation("Idempotency-Key", $"stress-void-par-{i}");
            return await _client.SendAsync(req);
        }

        var responses = await Task.WhenAll(Enumerable.Range(0, 15).Select(VoidOnce));
        Assert.DoesNotContain(responses, r => (int)r.StatusCode >= 500);
        var okCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        var conflictCount = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(15, okCount + conflictCount);
        Assert.Equal(1, okCount);
        Assert.Equal(14, conflictCount);

        await using var verify = _factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<PosDbContext>();
        var order = await db.Orders.AsNoTracking().FirstAsync(o => o.Id == orderId);
        Assert.Equal(OrderStatus.Void, order.Status);
        var voidRef = $"Order-{order.OrderNumber}-Void";
        Assert.Equal(
            1,
            await db.InventoryMovements.CountAsync(m =>
                m.Reference == voidRef && m.MovementType == InventoryMovementType.Return));
        Assert.Equal(1, await db.PaymentRefunds.CountAsync(r => r.OrderId == orderId));
    }

    [SkippableFact]
    public async Task Lifecycle_finalize_after_committed_void_is_conflict_with_reconciliation_visibility()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId();

        Guid menuItemId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<PosDbContext>();
            menuItemId = await ctx.MenuItems.Select(m => m.Id).FirstAsync();
        }

        var create = await _client.PostAsJsonAsync("/api/v1.0/orders", BuildCreateOrderPayload(menuItemId, 9.00m, notes: "lifecycle-fin-after-void"));
        var orderId = await ReadCreatedOrderIdAsync(create);
        await TransitionOrderToOpenAsync(orderId);

        SetIdempotencyKey("lifecycle-void-first-001");
        var voidResponse = await _client.PostAsJsonAsync($"/api/v1.0/orders/{orderId}/void", new { reason = "void first" });
        voidResponse.EnsureSuccessStatusCode();

        var finReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1.0/orders/{orderId}/finalize")
        {
            Content = JsonContent.Create(new
            {
                Payments = new[]
                {
                    new { PaymentMethod = "Cash", Amount = TotalWithLegacyTax(9.00m), TransactionId = "LIFECYCLE-FIN-AFTER-VOID" }
                }
            })
        };
        finReq.Headers.TryAddWithoutValidation("Idempotency-Key", "lifecycle-fin-after-void-001");
        var finResponse = await _client.SendAsync(finReq);
        Assert.Equal(HttpStatusCode.Conflict, finResponse.StatusCode);

        await using var verify = _factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<PosDbContext>();
        Assert.Equal(OrderStatus.Void, await db.Orders.Where(o => o.Id == orderId).Select(o => o.Status).SingleAsync());
        Assert.NotEmpty(await db.SyncConflictRecords
            .Where(c => c.EntityId == orderId && c.ConflictType == SyncConflictTypes.StaleOfflineMutation)
            .ToListAsync());
        Assert.NotEmpty(await db.OperationalAuditRecords
            .Where(a => a.OrderId == orderId && a.Action == OperationalAuditActions.StaleOfflineMutation)
            .ToListAsync());
    }

    [SkippableFact]
    public async Task Lifecycle_void_after_finalize_replay_same_key_is_deterministic_and_non_duplicating()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId();

        var seed = await SeedRecipeOrderForStressAsync();
        SetIdempotencyKey("lifecycle-fin-then-void-prep");
        var create = await _client.PostAsJsonAsync(
            "/api/v1.0/orders",
            BuildCreateOrderPayload(seed.MenuItemId, 15.99m, quantity: 2, notes: "lifecycle-fin-then-void"));
        var orderId = await ReadCreatedOrderIdAsync(create);
        await TransitionOrderToOpenAsync(orderId);

        var fin = await _client.PostAsJsonAsync(
            $"/api/v1.0/orders/{orderId}/finalize",
            new
            {
                Payments = new[]
                {
                    new { PaymentMethod = "Cash", Amount = seed.PaymentAmount, TransactionId = "LIFECYCLE-FIN-THEN-VOID" }
                }
            });
        fin.EnsureSuccessStatusCode();

        const string voidReplayKey = "lifecycle-void-replay-001";
        var voidRequest = new { reason = "void replay deterministic" };
        var req1 = new HttpRequestMessage(HttpMethod.Post, $"/api/v1.0/orders/{orderId}/void")
        {
            Content = JsonContent.Create(voidRequest)
        };
        req1.Headers.TryAddWithoutValidation("Idempotency-Key", voidReplayKey);
        var req2 = new HttpRequestMessage(HttpMethod.Post, $"/api/v1.0/orders/{orderId}/void")
        {
            Content = JsonContent.Create(voidRequest)
        };
        req2.Headers.TryAddWithoutValidation("Idempotency-Key", voidReplayKey);

        var r1 = await _client.SendAsync(req1);
        var r2 = await _client.SendAsync(req2);
        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, r2.StatusCode);

        await using var verify = _factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<PosDbContext>();
        var order = await db.Orders.AsNoTracking().FirstAsync(o => o.Id == orderId);
        Assert.Equal(OrderStatus.Void, order.Status);
        Assert.Equal(1, await db.PaymentRefunds.CountAsync(r => r.OrderId == orderId));
        var voidRef = $"Order-{order.OrderNumber}-Void";
        Assert.Equal(1, await db.InventoryMovements.CountAsync(m => m.Reference == voidRef && m.MovementType == InventoryMovementType.Return));
        Assert.Equal(1, await db.IdempotentRequests.CountAsync(r => r.Key == voidReplayKey && r.Endpoint == $"POST /api/orders/{orderId}/void"));
    }

    [SkippableFact]
    public async Task Finalize_underpayment_audit_survives_after_409_and_order_stays_open()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId();

        Guid menuItemId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<PosDbContext>();
            menuItemId = await ctx.MenuItems.Select(m => m.Id).FirstAsync();
        }

        var create = await _client.PostAsJsonAsync("/api/v1.0/orders", BuildCreateOrderPayload(menuItemId, 15.00m, notes: "stress-underpay"));
        var orderId = await ReadCreatedOrderIdAsync(create);
        await TransitionOrderToOpenAsync(orderId);

        SetIdempotencyKey("stress-underpay-001");
        var badFin = await _client.PostAsJsonAsync(
            $"/api/v1.0/orders/{orderId}/finalize",
            new { Payments = new[] { new { PaymentMethod = "Cash", Amount = 0.01m, TransactionId = "STRESS-UNDER" } } });
        Assert.Equal(HttpStatusCode.Conflict, badFin.StatusCode);

        await using var verify = _factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<PosDbContext>();
        Assert.Equal(OrderStatus.Open, await db.Orders.Where(o => o.Id == orderId).Select(o => o.Status).SingleAsync());
        Assert.Equal(0, await db.Payments.CountAsync(p => p.OrderId == orderId));
        Assert.Contains(
            await db.OperationalAuditRecords
                .Where(r => r.OrderId == orderId && r.Action == OperationalAuditActions.SettlementUnderpaymentRejected)
                .ToListAsync(),
            _ => true);
    }

    [SkippableFact]
    public async Task Replay_mismatch_storm_dedupes_conflict_and_preserves_single_receipt()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId(StressDeviceId);

        Guid ingredientId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<PosDbContext>();
            var ingredient = new Ingredient
            {
                Id = Guid.NewGuid(),
                Name = "Stress Mismatch Ingredient",
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

        const string opId = "stress-replay-mismatch-001";
        var adjustBody = new
        {
            deviceId = StressDeviceId,
            operations = new object[]
            {
                new
                {
                    operationId = opId,
                    type = "AdjustInventory",
                    payload = new Dictionary<string, object?>
                    {
                        ["ingredientId"] = ingredientId.ToString(),
                        ["quantity"] = "1",
                        ["reason"] = "mismatch-seed"
                    }
                }
            }
        };
        (await _client.PostAsJsonAsync("/api/v1.0/sync/push", adjustBody)).EnsureSuccessStatusCode();

        var mismatchBody = new
        {
            deviceId = StressDeviceId,
            operations = new object[]
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
        var responses = await StormSyncPushAsync(_client, mismatchBody, StormCount);
        Assert.DoesNotContain(responses, r => (int)r.StatusCode >= 500);

        await using var verify = _factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<PosDbContext>();
        Assert.Equal(1, await db.SyncOperationReceipts.CountAsync(r => r.DeviceId == StressDeviceId && r.OperationId == opId));
        var mismatchConflicts = await db.SyncConflictRecords
            .Where(r => r.DeviceId == StressDeviceId && r.OperationId == opId && r.ConflictType == SyncConflictTypes.ReplayMismatch)
            .ToListAsync();
        Assert.Single(mismatchConflicts);
    }

    [SkippableFact]
    public async Task Forensic_device_export_under_audit_pressure_truncates_and_orders_deterministically()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId(StressDeviceId);

        var baseUtc = DateTime.UtcNow.AddMinutes(-30);
        const int auditRows = OperationalForensicSnapshotConstants.MaxAuditTimelineItems + 12;
        await using (var seedScope = _factory.Services.CreateAsyncScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<PosDbContext>();
            for (var i = 0; i < auditRows; i++)
            {
                db.OperationalAuditRecords.Add(new OperationalAuditRecord
                {
                    Category = OperationalAuditCategories.Replay,
                    Action = "StressTimelineSeed",
                    EntityType = "Stress",
                    DeviceId = StressDeviceId,
                    OperationId = $"stress-audit-bulk-{i}",
                    CorrelationId = $"stress-audit-bulk-{i}",
                    Severity = OperationalAuditSeverity.Information,
                    Summary = $"stress audit row {i}",
                    CreatedAtUtc = baseUtc.AddMilliseconds(i)
                });
            }

            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync($"{ExportBase}/device/{StressDeviceId}");
        response.EnsureSuccessStatusCode();
        var snapshot = await response.Content.ReadFromJsonAsync<OperationalForensicSnapshotDto>();
        Assert.NotNull(snapshot);
        Assert.True(snapshot!.TruncationFlags.AuditTimelineTruncated);
        Assert.Equal(OperationalForensicSnapshotConstants.MaxAuditTimelineItems, snapshot.AuditTimeline.Count);
        Assert.True(snapshot.AuditTimeline.SequenceEqual(snapshot.AuditTimeline.OrderBy(x => x.TimestampUtc).ThenBy(x => x.Id)));
        Assert.False(string.IsNullOrWhiteSpace(snapshot.TruncationSeverity));
        AssertNoSensitiveLeakage(snapshot);
    }

    private async Task<StressRecipeOrderSeed> SeedRecipeOrderForStressAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<PosDbContext>();

        var ingredient = new Ingredient
        {
            Id = Guid.NewGuid(),
            Name = "Stress Void Ingredient",
            Unit = "kg",
            CostPerUnit = 4m,
            IsActive = true
        };
        context.Ingredients.Add(ingredient);
        context.InventoryItems.Add(new InventoryItem
        {
            Id = Guid.NewGuid(),
            IngredientId = ingredient.Id,
            CurrentStock = 100m,
            MinimumStock = 1m,
            MaximumStock = 200m,
            AverageCost = 4m,
            Unit = "kg",
            LastUpdated = DateTime.UtcNow
        });

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Stress Void Category",
            IsActive = true,
            DisplayOrder = 99
        };
        context.Categories.Add(category);

        var menuItem = new MenuItem
        {
            Id = Guid.NewGuid(),
            Name = "Stress Void Item",
            Price = 15.99m,
            CategoryId = category.Id,
            IsActive = true,
            HasIngredients = true
        };
        context.MenuItems.Add(menuItem);

        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            MenuItemId = menuItem.Id,
            Name = "Stress Void Recipe",
            IsActive = true
        };
        context.Recipes.Add(recipe);
        context.RecipeLines.Add(new RecipeLine
        {
            Id = Guid.NewGuid(),
            RecipeId = recipe.Id,
            IngredientId = ingredient.Id,
            QuantityPerItem = 0.5m,
            Unit = "kg"
        });

        await context.SaveChangesAsync();
        return new StressRecipeOrderSeed(menuItem.Id, TotalWithLegacyTax(31.98m));
    }

    private sealed record StressRecipeOrderSeed(Guid MenuItemId, decimal PaymentAmount);
}
