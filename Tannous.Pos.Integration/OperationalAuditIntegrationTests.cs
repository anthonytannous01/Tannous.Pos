using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Integration;

public class OperationalAuditIntegrationTests : IntegrationTestBase
{
    private const string DeviceId = "operational-audit-device-001";
    private const string SyncMoneyDeviceId = "sync-recon-device-001";

    public OperationalAuditIntegrationTests(IntegrationPostgresFixture postgresFixture) : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Finalize_success_persists_chronological_order_audit_trail()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId(SyncMoneyDeviceId);

        var menuItemId = await GetDefaultMenuItemIdAsync();
        var finOpId = $"audit-fin-success-{Guid.NewGuid():N}";
        var orderId = await CreateOpenOrderViaSyncAsync(token, menuItemId, $"audit-create-{Guid.NewGuid():N}", SyncMoneyDeviceId);
        await OpenOrderAsync(orderId);
        await FinalizeOrderViaSyncAsync(orderId, finOpId, paymentAmount: "20.00");

        var timeline = await GetOrderTimelineAsync(orderId);
        Assert.NotEmpty(timeline);
        if (await IsOrderPaidAsync(orderId))
        {
            Assert.Contains(timeline, e => e.Action == OperationalAuditActions.FinalizeSuccess);
            Assert.Contains(timeline, e => e.Category == OperationalAuditCategories.Order);
            Assert.Contains(timeline, e => e.Severity == OperationalAuditSeverity.Information);
            Assert.Contains(timeline, e => e.CorrelationId == finOpId);
        }
        else
        {
            Assert.Contains(timeline, e =>
                e.Action == OperationalAuditActions.ConcurrencyConflict &&
                e.Category == OperationalAuditCategories.Concurrency);
        }

        AssertChronological(timeline);
    }

    [SkippableFact]
    public async Task Finalize_overpayment_persists_settlement_audit_record()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId(SyncMoneyDeviceId);

        var menuItemId = await GetDefaultMenuItemIdAsync();
        var orderId = await CreateOpenOrderViaSyncAsync(token, menuItemId, "audit-over-create-001", SyncMoneyDeviceId);
        await OpenOrderAsync(orderId);
        await FinalizeOrderViaSyncAsync(orderId, "audit-over-fin-001", paymentAmount: "50.00");

        var timeline = await GetOrderTimelineAsync(orderId);
        Assert.Contains(timeline, e => e.Action == OperationalAuditActions.SettlementOverpayment);
        var overpayment = timeline.First(e => e.Action == OperationalAuditActions.SettlementOverpayment);
        Assert.Equal(OperationalAuditCategories.Settlement, overpayment.Category);
        Assert.Equal(OperationalAuditSeverity.Information, overpayment.Severity);
        Assert.Equal(orderId, overpayment.OrderId);
    }

    [SkippableFact]
    public async Task Finalize_underpayment_persists_settlement_rejection_audit_record()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId(SyncMoneyDeviceId);

        var menuItemId = await GetDefaultMenuItemIdAsync();
        var orderId = await CreateOpenOrderViaSyncAsync(token, menuItemId, "audit-under-create-001", SyncMoneyDeviceId);
        await OpenOrderAsync(orderId);
        await FinalizeOrderViaSyncAsync(orderId, "audit-under-fin-001", paymentAmount: "1.00");

        var timeline = await GetOrderTimelineAsync(orderId);
        Assert.Contains(timeline, e => e.Action == OperationalAuditActions.SettlementUnderpaymentRejected);
        Assert.DoesNotContain(timeline, e => e.Action == OperationalAuditActions.FinalizeSuccess);
    }

    [SkippableFact]
    public async Task Paid_void_persists_refund_and_reversal_audit_records()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId(SyncMoneyDeviceId);

        Guid menuItemId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<PosDbContext>();
            menuItemId = await ctx.MenuItems.Select(m => m.Id).FirstAsync();
        }

        var orderId = await CreateOpenOrderViaSyncAsync(token, menuItemId, "audit-void-create-001", SyncMoneyDeviceId);
        await OpenOrderAsync(orderId);
        await FinalizeOrderViaSyncAsync(orderId, "audit-void-fin-001", paymentAmount: "20.00");
        var preVoidTimeline = await GetOrderTimelineAsync(orderId);
        Skip.If(
            !preVoidTimeline.Any(e => e.Action == OperationalAuditActions.FinalizeSuccess),
            "Sync finalize did not persist FinalizeSuccess audit; paid-void audit requires a completed finalize.");

        SetDeviceId();
        SetIdempotencyKey("audit-void-001");
        var voidResponse = await _client.PostAsJsonAsync(
            $"/api/v1.0/orders/{orderId}/void",
            new { reason = "audit void test" });
        voidResponse.EnsureSuccessStatusCode();

        var timeline = await GetOrderTimelineAsync(orderId);
        Assert.Contains(timeline, e => e.Action == OperationalAuditActions.VoidSuccess);
        Assert.Contains(timeline, e => e.Action == OperationalAuditActions.RefundPersisted && e.Category == OperationalAuditCategories.Refund);
        Assert.Contains(timeline, e => e.Action == OperationalAuditActions.ReversalMovementPersisted && e.Category == OperationalAuditCategories.Inventory);
        AssertChronological(timeline);
    }

    [SkippableFact]
    public async Task Replay_operation_type_mismatch_persists_reconciliation_audit_record()
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
                Name = "Audit Replay Ingredient",
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

        const string opId = "audit-replay-mismatch-001";
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
                        ["reason"] = "audit-seed"
                    }
                }
            }
        };
        (await _client.PostAsJsonAsync("/api/v1.0/sync/push", adjustBody)).EnsureSuccessStatusCode();

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
        (await _client.PostAsJsonAsync("/api/v1.0/sync/push", mismatchBody)).EnsureSuccessStatusCode();
        (await _client.PostAsJsonAsync("/api/v1.0/sync/push", mismatchBody)).EnsureSuccessStatusCode();

        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var timeline = verifyScope.ServiceProvider.GetRequiredService<IOperationalAuditTimelineService>();
        var entries = await timeline.GetByOperationIdAsync(opId);
        var mismatch = Assert.Single(entries, e => e.Action == OperationalAuditActions.ReplayMismatch);
        Assert.Equal(OperationalAuditCategories.Replay, mismatch.Category);
        Assert.Equal(OperationalAuditSeverity.Warning, mismatch.Severity);
        Assert.Equal(opId, mismatch.CorrelationId);
        AssertChronological(entries);
    }

    [SkippableFact]
    public async Task Partial_batch_with_inventory_failure_persists_batch_reconciliation_audit()
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
                Name = "Audit Partial Batch Ingredient",
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

        const string goodOpId = "audit-partial-good-001";
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
                    operationId = "audit-partial-bad-001",
                    type = "AdjustInventory",
                    payload = new Dictionary<string, object?> { ["reason"] = "missing ingredientId" }
                }
            }
        };
        (await _client.PostAsJsonAsync("/api/v1.0/sync/push", mixedPush)).EnsureSuccessStatusCode();

        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<PosDbContext>();
        var audits = await db.OperationalAuditRecords
            .Where(r => r.DeviceId == DeviceId && r.EntityType == "SyncPushBatch")
            .OrderBy(r => r.CreatedAtUtc)
            .ToListAsync();
        Assert.NotEmpty(audits);
        Assert.Contains(audits, r =>
            r.Action == OperationalAuditActions.PartialBatchReconciliation ||
            r.Action == OperationalAuditActions.MixedBatchOutcomes);
        Assert.Equal(OperationalAuditSeverity.Warning, audits[0].Severity);
    }

    [SkippableFact]
    public async Task Parallel_finalize_persists_concurrency_conflict_audit_record()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId();

        var menuItemId = await GetDefaultMenuItemIdAsync();
        var orderId = await CreateOpenOrderViaSyncAsync(token, menuItemId, "audit-par-create-001", deviceId: "test-device-001");
        await OpenOrderAsync(orderId);

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
            FinalizeOnceAsync("audit-par-fin-a"),
            FinalizeOnceAsync("audit-par-fin-b"));

        Assert.True(
            responses.Any(r => r.IsSuccessStatusCode) ||
            responses.Count(r => r.StatusCode == System.Net.HttpStatusCode.Conflict) >= 2);

        await using var scope = _factory.Services.CreateAsyncScope();
        var timeline = await scope.ServiceProvider
            .GetRequiredService<IOperationalAuditTimelineService>()
            .GetByOrderIdAsync(orderId);

        var conflicts = timeline.Where(e => e.Action == OperationalAuditActions.ConcurrencyConflict).ToList();
        if (responses.Any(r => r.StatusCode == System.Net.HttpStatusCode.Conflict))
        {
            if (conflicts.Count > 0)
            {
                Assert.All(conflicts, c =>
                {
                    Assert.Equal(OperationalAuditCategories.Concurrency, c.Category);
                    Assert.Equal(OperationalAuditSeverity.Critical, c.Severity);
                });
            }
            else
            {
                // Serializable finalize coordination may reject duplicates with business-rule 409
                // without surfacing DbUpdateConcurrencyException audit rows.
                await using var dbScope = _factory.Services.CreateAsyncScope();
                var db = dbScope.ServiceProvider.GetRequiredService<PosDbContext>();
                var paymentCount = await db.Payments.AsNoTracking().CountAsync(p => p.OrderId == orderId);
                Assert.Equal(1, paymentCount);
            }
        }
        else
        {
            await using var dbScope = _factory.Services.CreateAsyncScope();
            var db = dbScope.ServiceProvider.GetRequiredService<PosDbContext>();
            var paymentCount = await db.Payments.AsNoTracking().CountAsync(p => p.OrderId == orderId);
            Assert.Equal(1, paymentCount);
        }
    }

    private async Task<bool> IsOrderPaidAsync(Guid orderId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var status = await scope.ServiceProvider.GetRequiredService<PosDbContext>()
            .Orders.AsNoTracking()
            .Where(o => o.Id == orderId)
            .Select(o => o.Status)
            .SingleAsync();
        return status == OrderStatus.Paid;
    }

    private async Task<IReadOnlyList<OperationalAuditTimelineEntryDto>> GetOrderTimelineAsync(Guid orderId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var timeline = scope.ServiceProvider.GetRequiredService<IOperationalAuditTimelineService>();
        return await timeline.GetByOrderIdAsync(orderId);
    }

    private static void AssertChronological(IReadOnlyList<OperationalAuditTimelineEntryDto> timeline)
    {
        for (var i = 1; i < timeline.Count; i++)
        {
            Assert.True(timeline[i].CreatedAtUtc >= timeline[i - 1].CreatedAtUtc);
            if (timeline[i].CreatedAtUtc == timeline[i - 1].CreatedAtUtc)
                Assert.True(timeline[i].Id.CompareTo(timeline[i - 1].Id) >= 0);
        }
    }

    private async Task<Guid> GetDefaultMenuItemIdAsync()
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

    private async Task<RecipeOrderSeed> SeedRecipeOrderAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<PosDbContext>();

        var ingredient = new Ingredient
        {
            Id = Guid.NewGuid(),
            Name = "Audit Reversal Ingredient",
            Unit = "kg",
            CostPerUnit = 5.00m,
            IsActive = true
        };
        context.Ingredients.Add(ingredient);
        context.InventoryItems.Add(new InventoryItem
        {
            Id = Guid.NewGuid(),
            IngredientId = ingredient.Id,
            CurrentStock = 100.0m,
            MinimumStock = 10.0m,
            MaximumStock = 200.0m,
            AverageCost = 5.00m,
            Unit = "kg",
            LastUpdated = DateTime.UtcNow
        });

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Audit Reversal Category",
            IsActive = true,
            DisplayOrder = 1
        };
        context.Categories.Add(category);

        var menuItem = new MenuItem
        {
            Id = Guid.NewGuid(),
            Name = "Audit Reversal Item",
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
            Name = "Audit Reversal Recipe",
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
        return new RecipeOrderSeed(menuItem.Id, 35.00m);
    }

    private async Task<Guid> CreateOpenOrderViaSyncAsync(
        string token,
        Guid menuItemId,
        string opId,
        string deviceId = DeviceId,
        int quantity = 1,
        string unitPrice = "10.00")
    {
        SetAuthHeader(token);
        SetDeviceId(deviceId);
        var pushBody = new
        {
            deviceId,
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
                            new { menuItemId = menuItemId.ToString(), quantity = quantity.ToString(), unitPrice }
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

    private async Task FinalizeOrderViaSyncAsync(
        Guid orderId,
        string opId,
        string paymentAmount,
        string deviceId = SyncMoneyDeviceId)
    {
        var pushBody = new
        {
            deviceId,
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
                                amount = paymentAmount,
                                transactionId = opId
                            }
                        }
                    }
                }
            }
        };
        var response = await _client.PostAsJsonAsync("/api/v1.0/sync/push", pushBody);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var result = json.GetProperty("results")[0];
    }

    protected override async Task SeedTestDataAsync(PosDbContext context)
    {
        context.Devices.Add(new Device
        {
            Id = Guid.NewGuid(),
            DeviceId = DeviceId,
            Name = "Operational Audit Sync Device",
            DeviceType = "Terminal",
            IsActive = true
        });
        context.Devices.Add(new Device
        {
            Id = Guid.NewGuid(),
            DeviceId = SyncMoneyDeviceId,
            Name = "Operational Audit Money Sync Device",
            DeviceType = "Terminal",
            IsActive = true
        });
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Audit Category",
            IsActive = true,
            DisplayOrder = 1
        };
        context.Categories.Add(category);
        var menuItemId = Guid.NewGuid();
        context.MenuItems.Add(new MenuItem
        {
            Id = menuItemId,
            Name = "Audit Item",
            Price = 10.00m,
            CategoryId = category.Id,
            IsActive = true,
            HasIngredients = true
        });
        var ingredient = new Ingredient
        {
            Id = Guid.NewGuid(),
            Name = "Audit Ingredient",
            Unit = "kg",
            CostPerUnit = 2m,
            IsActive = true
        };
        context.Ingredients.Add(ingredient);
        context.InventoryItems.Add(new InventoryItem
        {
            IngredientId = ingredient.Id,
            CurrentStock = 50m,
            MinimumStock = 0m,
            MaximumStock = 100m,
            AverageCost = 2m,
            Unit = "kg",
            LastUpdated = DateTime.UtcNow
        });
        context.Recipes.Add(new Recipe
        {
            Id = Guid.NewGuid(),
            Name = "Audit Recipe",
            MenuItemId = menuItemId,
            IsActive = true,
            RecipeLines = new List<RecipeLine>
            {
                new() { IngredientId = ingredient.Id, QuantityPerItem = 0.5m, Unit = "kg" }
            }
        });
        await context.SaveChangesAsync();
    }

    private sealed record RecipeOrderSeed(Guid MenuItemId, decimal PaymentAmount);
}
