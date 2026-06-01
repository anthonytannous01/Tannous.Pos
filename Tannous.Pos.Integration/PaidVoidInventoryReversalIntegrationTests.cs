using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Integration;

public class PaidVoidInventoryReversalIntegrationTests : IntegrationTestBase
{
    public PaidVoidInventoryReversalIntegrationTests(IntegrationPostgresFixture postgresFixture) : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Paid_void_restores_stock_from_finalize_sale_movements()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId();

        var seed = await SeedRecipeOrderAsync();
        var orderId = await CreateAndFinalizeOrderAsync(token, seed.MenuItemId, seed.PaymentAmount, "void-restore-001");

        await using (var beforeVoid = _factory.Services.CreateAsyncScope())
        {
            var db = beforeVoid.ServiceProvider.GetRequiredService<PosDbContext>();
            var stockAfterFinalize = await db.InventoryItems
                .Where(ii => ii.IngredientId == seed.IngredientId)
                .Select(ii => ii.CurrentStock)
                .SingleAsync();
            Assert.Equal(99.0m, stockAfterFinalize);
        }

        SetIdempotencyKey("void-restore-001");
        var voidResponse = await _client.PostAsJsonAsync(
            $"/api/v1.0/orders/{orderId}/void",
            new { reason = "customer cancel" });
        voidResponse.EnsureSuccessStatusCode();

        await using var verify = _factory.Services.CreateAsyncScope();
        var context = verify.ServiceProvider.GetRequiredService<PosDbContext>();
        var order = await context.Orders.AsNoTracking().FirstAsync(o => o.Id == orderId);
        Assert.Equal(OrderStatus.Void, order.Status);

        var stockAfterVoid = await context.InventoryItems
            .Where(ii => ii.IngredientId == seed.IngredientId)
            .Select(ii => ii.CurrentStock)
            .SingleAsync();
        Assert.Equal(100.0m, stockAfterVoid);

        var voidReference = $"Order-{order.OrderNumber}-Void";
        var reversals = await context.InventoryMovements
            .Where(m => m.Reference == voidReference && m.MovementType == InventoryMovementType.Return)
            .ToListAsync();
        Assert.Single(reversals);
        Assert.Equal(1.0m, reversals[0].Quantity);
        Assert.NotNull(reversals[0].ReversedMovementId);
    }

    [SkippableFact]
    public async Task Paid_void_idempotent_retry_does_not_double_restore_stock()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId();

        var seed = await SeedRecipeOrderAsync();
        var orderId = await CreateAndFinalizeOrderAsync(token, seed.MenuItemId, seed.PaymentAmount, "void-idem-finalize-001");

        SetIdempotencyKey("void-idem-001");
        var voidBody = new { reason = "duplicate client retry" };
        var first = await _client.PostAsJsonAsync($"/api/v1.0/orders/{orderId}/void", voidBody);
        var second = await _client.PostAsJsonAsync($"/api/v1.0/orders/{orderId}/void", voidBody);
        first.EnsureSuccessStatusCode();
        second.EnsureSuccessStatusCode();

        await using var verify = _factory.Services.CreateAsyncScope();
        var context = verify.ServiceProvider.GetRequiredService<PosDbContext>();
        var order = await context.Orders.AsNoTracking().FirstAsync(o => o.Id == orderId);
        var voidReference = $"Order-{order.OrderNumber}-Void";
        var reversalCount = await context.InventoryMovements
            .CountAsync(m => m.Reference == voidReference && m.MovementType == InventoryMovementType.Return);
        Assert.Equal(1, reversalCount);

        var stock = await context.InventoryItems
            .Where(ii => ii.IngredientId == seed.IngredientId)
            .Select(ii => ii.CurrentStock)
            .SingleAsync();
        Assert.Equal(100.0m, stock);
    }

    [SkippableFact]
    public async Task Second_void_with_different_idempotency_key_fails_without_extra_reversals()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId();

        var seed = await SeedRecipeOrderAsync();
        var orderId = await CreateAndFinalizeOrderAsync(token, seed.MenuItemId, seed.PaymentAmount, "void-second-001");

        SetIdempotencyKey("void-second-a");
        var first = await _client.PostAsJsonAsync($"/api/v1.0/orders/{orderId}/void", new { reason = "first void" });
        first.EnsureSuccessStatusCode();

        SetIdempotencyKey("void-second-b");
        var second = await _client.PostAsJsonAsync($"/api/v1.0/orders/{orderId}/void", new { reason = "second void" });
        Assert.NotEqual(HttpStatusCode.OK, second.StatusCode);

        await using var verify = _factory.Services.CreateAsyncScope();
        var context = verify.ServiceProvider.GetRequiredService<PosDbContext>();
        var order = await context.Orders.AsNoTracking().FirstAsync(o => o.Id == orderId);
        var voidReference = $"Order-{order.OrderNumber}-Void";
        Assert.Equal(1, await context.InventoryMovements.CountAsync(m =>
            m.Reference == voidReference && m.MovementType == InventoryMovementType.Return));
    }

    [SkippableFact]
    public async Task Parallel_void_attempts_preserve_single_reversal_set()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId();

        var seed = await SeedRecipeOrderAsync();
        var orderId = await CreateAndFinalizeOrderAsync(token, seed.MenuItemId, seed.PaymentAmount, "void-parallel-fin-001");

        async Task<HttpResponseMessage> VoidOnce(string idemKey)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, $"/api/v1.0/orders/{orderId}/void")
            {
                Content = JsonContent.Create(new { reason = "parallel void" })
            };
            req.Headers.TryAddWithoutValidation("Idempotency-Key", idemKey);
            return await _client.SendAsync(req);
        }

        var responses = await Task.WhenAll(VoidOnce("void-par-a"), VoidOnce("void-par-b"));
        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        Assert.Equal(1, successCount);

        await using var verify = _factory.Services.CreateAsyncScope();
        var context = verify.ServiceProvider.GetRequiredService<PosDbContext>();
        var order = await context.Orders.AsNoTracking().FirstAsync(o => o.Id == orderId);
        Assert.Equal(OrderStatus.Void, order.Status);
        var voidReference = $"Order-{order.OrderNumber}-Void";
        Assert.Equal(1, await context.InventoryMovements.CountAsync(m =>
            m.Reference == voidReference && m.MovementType == InventoryMovementType.Return));

        var stock = await context.InventoryItems
            .Where(ii => ii.IngredientId == seed.IngredientId)
            .Select(ii => ii.CurrentStock)
            .SingleAsync();
        Assert.Equal(100.0m, stock);
    }

    [SkippableFact]
    public async Task Paid_void_stale_inventory_row_surfaces_safe_concurrency_error()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId();

        var seed = await SeedRecipeOrderAsync();
        var orderId = await CreateAndFinalizeOrderAsync(token, seed.MenuItemId, seed.PaymentAmount, "void-conc-fin-001");

        Guid inventoryItemId;
        await using (var idScope = _factory.Services.CreateAsyncScope())
        {
            var idCtx = idScope.ServiceProvider.GetRequiredService<PosDbContext>();
            inventoryItemId = await idCtx.InventoryItems
                .Where(ii => ii.IngredientId == seed.IngredientId)
                .Select(ii => ii.Id)
                .SingleAsync();
        }

        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<PosDbContext>();
        var item = await ctx.InventoryItems.FirstAsync(ii => ii.Id == inventoryItemId);

        // Simulate a stale client RowVersion (never refreshed after another writer updated the row).
        ctx.Entry(item).Property(ii => ii.RowVersion).OriginalValue = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        item.CurrentStock += 0.01m;

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => ctx.SaveChangesAsync());
    }

    private async Task<RecipeOrderSeed> SeedRecipeOrderAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<PosDbContext>();

        var ingredient = new Ingredient
        {
            Id = Guid.NewGuid(),
            Name = "Void Reversal Ingredient",
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
            Name = "Void Reversal Category",
            IsActive = true,
            DisplayOrder = 1
        };
        context.Categories.Add(category);

        var menuItem = new MenuItem
        {
            Id = Guid.NewGuid(),
            Name = "Void Reversal Item",
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
            Name = "Void Reversal Recipe",
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

        return new RecipeOrderSeed(ingredient.Id, menuItem.Id, TotalWithLegacyTax(31.98m));
    }

    private async Task<Guid> CreateAndFinalizeOrderAsync(
        string token,
        Guid menuItemId,
        decimal paymentAmount,
        string finalizeIdempotencyKey)
    {
        SetIdempotencyKey(finalizeIdempotencyKey);
        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1.0/orders",
            BuildCreateOrderPayload(menuItemId, 15.99m, quantity: 2, notes: "void reversal integration order"));
        var orderId = await ReadCreatedOrderIdAsync(createResponse);
        await TransitionOrderToOpenAsync(orderId);

        var finalizeResponse = await _client.PostAsJsonAsync(
            $"/api/v1.0/orders/{orderId}/finalize",
            new
            {
                Payments = new[]
                {
                    new { PaymentMethod = "Cash", Amount = paymentAmount, TransactionId = $"TXN-{finalizeIdempotencyKey}" }
                }
            });
        finalizeResponse.EnsureSuccessStatusCode();
        return orderId;
    }

    private sealed record RecipeOrderSeed(Guid IngredientId, Guid MenuItemId, decimal PaymentAmount);
}
