using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Integration;

/// <summary>
/// Deterministic optimistic concurrency checks (two DbContext instances), independent of HTTP timing.
/// </summary>
public class OptimisticConcurrencyIntegrationTests : IntegrationTestBase
{
    public OptimisticConcurrencyIntegrationTests(IntegrationPostgresFixture postgresFixture) : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task InventoryItem_second_save_with_stale_row_throws_DbUpdateConcurrencyException()
    {
        await InitializeDatabaseAsync();

        Guid ingredientId;
        Guid inventoryItemId;

        using (var seedScope = _factory.Services.CreateScope())
        {
            var ctx = seedScope.ServiceProvider.GetRequiredService<PosDbContext>();
            var ingredient = new Ingredient
            {
                Id = Guid.NewGuid(),
                Name = "ConcurrencyIngredient",
                Unit = "kg",
                CostPerUnit = 1m,
                IsActive = true
            };
            ctx.Ingredients.Add(ingredient);
            ingredientId = ingredient.Id;

            var item = new InventoryItem
            {
                IngredientId = ingredient.Id,
                CurrentStock = 10m,
                MinimumStock = 0m,
                MaximumStock = 100m,
                AverageCost = 1m,
                Unit = "kg",
                LastUpdated = DateTime.UtcNow
            };
            ctx.InventoryItems.Add(item);
            await ctx.SaveChangesAsync();
            inventoryItemId = item.Id;
        }

        using var scopeA = _factory.Services.CreateScope();
        using var scopeB = _factory.Services.CreateScope();
        var ctxA = scopeA.ServiceProvider.GetRequiredService<PosDbContext>();
        var ctxB = scopeB.ServiceProvider.GetRequiredService<PosDbContext>();

        var itemA = await ctxA.InventoryItems.FirstAsync(ii => ii.Id == inventoryItemId);
        var itemB = await ctxB.InventoryItems.FirstAsync(ii => ii.Id == inventoryItemId);

        itemA.CurrentStock += 1m;
        await ctxA.SaveChangesAsync();

        itemB.CurrentStock -= 1m;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => ctxB.SaveChangesAsync());
    }
}
