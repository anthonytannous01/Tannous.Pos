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

public class OperationalAuditDiagnosticsIntegrationTests : IntegrationTestBase
{
    private const string DiagnosticsBase = "/api/v1.0/internal/operational-audit";
    private const string SyncDeviceId = "diag-audit-device-001";

    public OperationalAuditDiagnosticsIntegrationTests(IntegrationPostgresFixture postgresFixture) : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Order_timeline_endpoint_returns_audit_records_after_finalize_attempt()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId(SyncDeviceId);

        var menuItemId = await GetMenuItemIdAsync();
        var orderId = await CreateOpenOrderViaSyncAsync(menuItemId, $"diag-create-{Guid.NewGuid():N}");
        await OpenOrderAsync(orderId);
        await FinalizeViaSyncAsync(orderId, $"diag-fin-{Guid.NewGuid():N}");

        var response = await _client.GetAsync($"{DiagnosticsBase}/timeline/order/{orderId}?page=1&pageSize=50");
        response.EnsureSuccessStatusCode();

        var page = await response.Content.ReadFromJsonAsync<OperationalAuditPageDto>();
        Assert.NotNull(page);
        Assert.True(page!.Total >= 1);
        Assert.NotEmpty(page.Items);
        Assert.True(page.Items.SequenceEqual(page.Items.OrderBy(i => i.TimestampUtc)));
        Assert.DoesNotContain(page.Items, i => i.Message.Contains("stack", StringComparison.OrdinalIgnoreCase));
    }

    [SkippableFact]
    public async Task Operation_timeline_returns_replay_mismatch_after_type_mismatch_push()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId(SyncDeviceId);

        Guid ingredientId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<PosDbContext>();
            var ingredient = new Ingredient
            {
                Id = Guid.NewGuid(),
                Name = "Diag Ingredient",
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

        const string opId = "diag-replay-mismatch-001";
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
                        ["reason"] = "diag"
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

        var response = await _client.GetAsync($"{DiagnosticsBase}/timeline/operation/{opId}");
        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<OperationalAuditPageDto>();
        Assert.NotNull(page);
        Assert.Contains(page!.Items, i => i.Action == OperationalAuditActions.ReplayMismatch);
    }

    [SkippableFact]
    public async Task Recent_conflicts_endpoint_surfaces_partial_batch_reconciliation()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId(SyncDeviceId);

        Guid ingredientId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<PosDbContext>();
            var ingredient = new Ingredient
            {
                Id = Guid.NewGuid(),
                Name = "Diag Partial Batch",
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

        const string goodOpId = "diag-partial-good-001";
        var seedPush = new
        {
            deviceId = SyncDeviceId,
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
            deviceId = SyncDeviceId,
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
                    operationId = "diag-partial-bad-001",
                    type = "AdjustInventory",
                    payload = new Dictionary<string, object?> { ["reason"] = "missing ingredientId" }
                }
            }
        };
        (await _client.PostAsJsonAsync("/api/v1.0/sync/push", mixedPush)).EnsureSuccessStatusCode();

        var response = await _client.GetAsync($"{DiagnosticsBase}/conflicts/recent?page=1&pageSize=50");
        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<OperationalAuditPageDto>();
        Assert.NotNull(page);
        Assert.True(page!.Items.Any(i =>
            i.Action == OperationalAuditActions.PartialBatchReconciliation
            || i.Action == OperationalAuditActions.MixedBatchOutcomes));
    }

    [SkippableFact]
    public async Task Pagination_enforces_max_page_size()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        var response = await _client.GetAsync($"{DiagnosticsBase}/conflicts/recent?page=1&pageSize=500");
        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<OperationalAuditPageDto>();
        Assert.NotNull(page);
        Assert.Equal(OperationalAuditQueryConstants.MaxPageSize, page!.PageSize);
    }

    [SkippableFact]
    public async Task Cashier_user_is_denied_diagnostics_endpoints()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync("cashier", "password");
        SetAuthHeader(token);

        var response = await _client.GetAsync($"{DiagnosticsBase}/conflicts/recent");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [SkippableFact]
    public async Task Device_timeline_returns_records_ordered_by_timestamp()
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
                    operationId = "diag-device-op-001",
                    type = "CreateCustomer",
                    payload = new Dictionary<string, object?>()
                }
            }
        };
        (await _client.PostAsJsonAsync("/api/v1.0/sync/push", pushBody)).EnsureSuccessStatusCode();

        var response = await _client.GetAsync($"{DiagnosticsBase}/timeline/device/{SyncDeviceId}");
        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<OperationalAuditPageDto>();
        Assert.NotNull(page);
        Assert.NotEmpty(page!.Items);
        Assert.True(page.Items.SequenceEqual(page.Items.OrderBy(i => i.TimestampUtc)));
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
            Name = "Diagnostics Device",
            DeviceType = "Terminal",
            IsActive = true
        });
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Diagnostics Category",
            IsActive = true,
            DisplayOrder = 1
        };
        context.Categories.Add(category);
        context.MenuItems.Add(new MenuItem
        {
            Id = Guid.NewGuid(),
            Name = "Diagnostics Item",
            Price = 10m,
            CategoryId = category.Id,
            IsActive = true
        });
        await context.SaveChangesAsync();
    }
}
