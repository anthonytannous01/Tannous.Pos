using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Integration;

public class PaidVoidRefundIntegrationTests : IntegrationTestBase
{
    public PaidVoidRefundIntegrationTests(IntegrationPostgresFixture postgresFixture) : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task Paid_void_creates_single_refund_matching_payments()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId();

        var seed = await SeedRecipeOrderAsync();
        var orderId = await CreateAndFinalizeOrderAsync(token, seed.MenuItemId, seed.PaymentAmount, "refund-fin-001");

        SetIdempotencyKey("refund-void-001");
        var voidResponse = await _client.PostAsJsonAsync(
            $"/api/v1.0/orders/{orderId}/void",
            new { reason = "refund test" });
        voidResponse.EnsureSuccessStatusCode();

        await using var verify = _factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<PosDbContext>();
        var refunds = await db.PaymentRefunds.Where(r => r.OrderId == orderId).ToListAsync();
        Assert.Single(refunds);
        Assert.Equal(Math.Round(seed.PaymentAmount, 2), Math.Round(refunds[0].Amount, 2));
        Assert.Equal("refund-void-001", refunds[0].CorrelationId);

        var payments = await db.Payments.Where(p => p.OrderId == orderId).ToListAsync();
        Assert.Equal(Math.Round(seed.PaymentAmount, 2), Math.Round(payments.Sum(p => p.Amount), 2));
    }

    [SkippableFact]
    public async Task Paid_void_idempotent_retry_does_not_duplicate_refund()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId();

        var seed = await SeedRecipeOrderAsync();
        var orderId = await CreateAndFinalizeOrderAsync(token, seed.MenuItemId, seed.PaymentAmount, "refund-idem-fin-001");

        SetIdempotencyKey("refund-idem-void-001");
        var body = new { reason = "idem refund" };
        var first = await _client.PostAsJsonAsync($"/api/v1.0/orders/{orderId}/void", body);
        var second = await _client.PostAsJsonAsync($"/api/v1.0/orders/{orderId}/void", body);
        first.EnsureSuccessStatusCode();
        second.EnsureSuccessStatusCode();

        await using var verify = _factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<PosDbContext>();
        Assert.Equal(1, await db.PaymentRefunds.CountAsync(r => r.OrderId == orderId));
    }

    [SkippableFact]
    public async Task Paid_void_refund_and_inventory_reversal_atomic()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId();

        var seed = await SeedRecipeOrderAsync();
        var orderId = await CreateAndFinalizeOrderAsync(token, seed.MenuItemId, seed.PaymentAmount, "refund-atom-fin-001");

        SetIdempotencyKey("refund-atom-void-001");
        var voidResponse = await _client.PostAsJsonAsync(
            $"/api/v1.0/orders/{orderId}/void",
            new { reason = "atomic refund reversal" });
        voidResponse.EnsureSuccessStatusCode();

        await using var verify = _factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<PosDbContext>();
        var order = await db.Orders.AsNoTracking().FirstAsync(o => o.Id == orderId);
        Assert.Equal(OrderStatus.Void, order.Status);
        Assert.Single(await db.PaymentRefunds.Where(r => r.OrderId == orderId).ToListAsync());

        var voidReference = $"Order-{order.OrderNumber}-Void";
        Assert.Single(await db.InventoryMovements
            .Where(m => m.Reference == voidReference && m.MovementType == InventoryMovementType.Return)
            .ToListAsync());

        var stock = await db.InventoryItems.Where(ii => ii.IngredientId == seed.IngredientId)
            .Select(ii => ii.CurrentStock)
            .SingleAsync();
        Assert.Equal(100.0m, stock);
    }

    [SkippableFact]
    public async Task Parallel_void_attempts_preserve_single_refund()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId();

        var seed = await SeedRecipeOrderAsync();
        var orderId = await CreateAndFinalizeOrderAsync(token, seed.MenuItemId, seed.PaymentAmount, "refund-par-fin-001");

        async Task<HttpResponseMessage> VoidOnce(string idemKey)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, $"/api/v1.0/orders/{orderId}/void")
            {
                Content = JsonContent.Create(new { reason = "parallel refund void" })
            };
            req.Headers.TryAddWithoutValidation("Idempotency-Key", idemKey);
            return await _client.SendAsync(req);
        }

        var responses = await Task.WhenAll(VoidOnce("refund-par-a"), VoidOnce("refund-par-b"));
        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.OK));

        await using var verify = _factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<PosDbContext>();
        Assert.Equal(1, await db.PaymentRefunds.CountAsync(r => r.OrderId == orderId));
    }

    private async Task<RecipeOrderSeed> SeedRecipeOrderAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<PosDbContext>();

        var ingredient = new Ingredient
        {
            Id = Guid.NewGuid(),
            Name = "Refund Test Ingredient",
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
            Name = "Refund Category",
            IsActive = true,
            DisplayOrder = 1
        };
        context.Categories.Add(category);

        var menuItem = new MenuItem
        {
            Id = Guid.NewGuid(),
            Name = "Refund Item",
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
            Name = "Refund Recipe",
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
            BuildCreateOrderPayload(menuItemId, 15.99m, quantity: 2, notes: "refund integration order"));
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
