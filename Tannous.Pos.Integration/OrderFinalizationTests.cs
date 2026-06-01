using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Integration;

public class OrderFinalizationTests : IntegrationTestBase
{
    public OrderFinalizationTests(IntegrationPostgresFixture postgresFixture) : base(postgresFixture)
    {
    }

    [SkippableFact]
    public async Task OrderFinalization_ShouldAssignReceiptNumber_AndCreateInventoryDeductions()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId();
        SetIdempotencyKey("test-finalize-001");

        // Create an order first
        var menuItemId = await GetDefaultMenuItemIdAsync();
        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1.0/orders",
            BuildCreateOrderPayload(menuItemId, 10.99m, quantity: 2, notes: "Test order"));
        var orderId = await ReadCreatedOrderIdAsync(createResponse);
        await TransitionOrderToOpenAsync(orderId);

        // Act - Finalize the order
        var finalizeRequest = new
        {
            Payments = new[]
            {
                new
                {
                    PaymentMethod = "Cash",
                    Amount = TotalWithLegacyTax(21.98m),
                    TransactionId = "TXN001"
                }
            }
        };

        var finalizeResponse = await _client.PostAsJsonAsync($"/api/v1.0/orders/{orderId}/finalize", finalizeRequest);

        // Assert
        await AssertHttpSuccessAsync(finalizeResponse, "finalize order");
        var finalizedOrder = await finalizeResponse.Content.ReadFromJsonAsync<JsonElement>();
        var hasReceipt =
            (finalizedOrder.TryGetProperty("receiptNumber", out var receipt) ||
             finalizedOrder.TryGetProperty("ReceiptNumber", out receipt)) &&
            !string.IsNullOrWhiteSpace(receipt.GetString());
        Assert.True(hasReceipt);

        // Verify inventory deductions were created
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PosDbContext>();
        
        var dbOrder = await context.Orders.FindAsync(orderId);
        var orderNumber = dbOrder?.OrderNumber ?? string.Empty;
        var allMovements = await context.InventoryMovements.ToListAsync();
        var inventoryMovements = allMovements
            .Where(im => im.Reference != null && im.Reference.Contains(orderNumber))
            .ToList();

        Assert.NotEmpty(inventoryMovements);
    }

    [SkippableFact]
    public async Task OrderFinalization_ShouldBeIdempotent_WhenOrderAlreadyFinalized()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId();
        
        // Create and finalize an order
        var orderId = await CreateAndFinalizeOrderAsync(token, "test-idempotent-001");
        
        // Act - Try to finalize the same order again with same idempotency key
        SetIdempotencyKey("test-idempotent-001");
        var finalizeRequest = new
        {
            Payments = new[]
            {
                new { PaymentMethod = "Cash", Amount = TotalWithLegacyTax(21.98m), TransactionId = "TXN002" }
            }
        };

        var finalizeResponse = await _client.PostAsJsonAsync($"/api/v1.0/orders/{orderId}/finalize", finalizeRequest);

        // Assert - Should return 200 with same order state (idempotent)
        Assert.Equal(HttpStatusCode.OK, finalizeResponse.StatusCode);
        var finalizedOrder = await finalizeResponse.Content.ReadFromJsonAsync<dynamic>();
        
        // Verify order is still in Paid status
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PosDbContext>();
        var order = await context.Orders.FindAsync(orderId);
        
        Assert.NotNull(order);
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.NotNull(order.ReceiptNumber);
    }

    [SkippableFact]
    public async Task OrderFinalization_ShouldRollback_OnPaymentValidationFailure()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId();
        SetIdempotencyKey("test-rollback-001");

        // Create an order
        var orderId = await CreateTestOrderAsync(token);
        await TransitionOrderToOpenAsync(orderId);

        // Act - Try to finalize with insufficient payment (should fail and rollback)
        var finalizeRequest = new
        {
            Payments = new[]
            {
                new { PaymentMethod = "Cash", Amount = 1.00m, TransactionId = "TXN003" } // Insufficient
            }
        };

        var finalizeResponse = await _client.PostAsJsonAsync($"/api/v1.0/orders/{orderId}/finalize", finalizeRequest);

        // Assert - Should return error
        Assert.NotEqual(HttpStatusCode.OK, finalizeResponse.StatusCode);

        // Verify order is NOT finalized (transaction rolled back)
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PosDbContext>();
        var order = await context.Orders
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == orderId);
        
        Assert.NotNull(order);
        Assert.NotEqual(OrderStatus.Paid, order.Status); // Should still be Open
        Assert.Empty(order.Payments); // No payments should be created
        Assert.Null(order.ReceiptNumber); // No receipt number assigned
    }

    private async Task<Guid> CreateTestOrderAsync(string token)
    {
        var menuItemId = await GetDefaultMenuItemIdAsync();
        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1.0/orders",
            BuildCreateOrderPayload(menuItemId, 10.99m, quantity: 2, notes: "Test order"));
        return await ReadCreatedOrderIdAsync(createResponse);
    }

    private async Task<Guid> CreateAndFinalizeOrderAsync(string token, string idempotencyKey)
    {
        var orderId = await CreateTestOrderAsync(token);
        await TransitionOrderToOpenAsync(orderId);

        SetIdempotencyKey(idempotencyKey);
        var finalizeRequest = new
        {
            Payments = new[]
            {
                new { PaymentMethod = "Cash", Amount = TotalWithLegacyTax(21.98m), TransactionId = "TXN001" }
            }
        };

        var finalizeResponse = await _client.PostAsJsonAsync($"/api/v1.0/orders/{orderId}/finalize", finalizeRequest);
        finalizeResponse.EnsureSuccessStatusCode();
        
        return orderId;
    }

    protected override async Task SeedTestDataAsync(PosDbContext context)
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Seed Category",
            IsActive = true,
            DisplayOrder = 1
        };
        context.Categories.Add(category);

        var menuItemId = Guid.NewGuid();
        context.MenuItems.Add(new MenuItem
        {
            Id = menuItemId,
            Name = "Test Item",
            Description = "Test Description",
            Price = 10.99m,
            CategoryId = category.Id,
            IsActive = true
        });

        var ingredient = new Ingredient
        {
            Id = Guid.NewGuid(),
            Name = "Seed Ingredient",
            Unit = "kg",
            CostPerUnit = 2.50m,
            IsActive = true
        };
        context.Ingredients.Add(ingredient);
        context.InventoryItems.Add(new InventoryItem
        {
            IngredientId = ingredient.Id,
            CurrentStock = 100m,
            MinimumStock = 0m,
            MaximumStock = 500m,
            AverageCost = 2.50m,
            Unit = "kg",
            LastUpdated = DateTime.UtcNow
        });
        context.Recipes.Add(new Recipe
        {
            Id = Guid.NewGuid(),
            Name = "Seed Recipe",
            MenuItemId = menuItemId,
            IsActive = true,
            RecipeLines = new List<RecipeLine>
            {
                new()
                {
                    IngredientId = ingredient.Id,
                    QuantityPerItem = 0.5m,
                    Unit = "kg"
                }
            }
        });

        await context.SaveChangesAsync();
    }

    [SkippableFact]
    public async Task FinalizeOrder_ShouldCreateInventoryMovements_ForRecipeIngredients()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId();
        SetIdempotencyKey("test-inventory-001");

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PosDbContext>();

        // Create test data: ingredient, inventory item, menu item, recipe
        var ingredient = new Ingredient
        {
            Id = Guid.NewGuid(),
            Name = "Test Ingredient",
            Unit = "kg",
            CostPerUnit = 5.00m,
            IsActive = true
        };
        context.Ingredients.Add(ingredient);

        var inventoryItem = new InventoryItem
        {
            Id = Guid.NewGuid(),
            IngredientId = ingredient.Id,
            CurrentStock = 100.0m,
            MinimumStock = 10.0m,
            MaximumStock = 200.0m,
            AverageCost = 5.00m,
            Unit = "kg",
            LastUpdated = DateTime.UtcNow
        };
        context.InventoryItems.Add(inventoryItem);

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Test Category",
            IsActive = true,
            DisplayOrder = 1
        };
        context.Categories.Add(category);

        var menuItem = new MenuItem
        {
            Id = Guid.NewGuid(),
            Name = "Test Menu Item",
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
            Name = "Test Recipe",
            IsActive = true
        };
        context.Recipes.Add(recipe);

        var recipeLine = new RecipeLine
        {
            Id = Guid.NewGuid(),
            RecipeId = recipe.Id,
            IngredientId = ingredient.Id,
            QuantityPerItem = 0.5m, // 0.5 kg per menu item
            Unit = "kg"
        };
        context.RecipeLines.Add(recipeLine);

        await context.SaveChangesAsync();

        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1.0/orders",
            BuildCreateOrderPayload(menuItem.Id, 15.99m, quantity: 2, notes: "Test order for inventory"));
        var orderId = await ReadCreatedOrderIdAsync(createResponse);
        await TransitionOrderToOpenAsync(orderId);

        // Act - Finalize the order
        var finalizeRequest = new
        {
            Payments = new[]
            {
                new { PaymentMethod = "Cash", Amount = TotalWithLegacyTax(31.98m), TransactionId = "TXN-INV-001" }
            }
        };

        var finalizeResponse = await _client.PostAsJsonAsync($"/api/v1.0/orders/{orderId}/finalize", finalizeRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, finalizeResponse.StatusCode);

        var movements = await GetInventoryMovementsForOrderAsync(orderId);
        Assert.Single(movements);
        var movement = movements.First();
        Assert.Equal(InventoryMovementType.Sale, movement.MovementType);
        Assert.Equal(-1.0m, movement.Quantity); // 0.5 kg * 2 items = 1.0 kg (negative for deduction)
        Assert.Equal(ingredient.Id, movement.IngredientId);

        var updatedInventory = await GetInventoryItemByIngredientAsync(ingredient.Id);
        Assert.NotNull(updatedInventory);
        Assert.Equal(99.0m, updatedInventory.CurrentStock); // 100 - 1.0 = 99
    }

    [SkippableFact]
    public async Task FinalizeOrder_ShouldDeductCorrectQuantities_ForMultipleOrderLines()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId();
        SetIdempotencyKey("test-inventory-multi-001");

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PosDbContext>();

        // Create test data
        var ingredient = new Ingredient
        {
            Id = Guid.NewGuid(),
            Name = "Shared Ingredient",
            Unit = "kg",
            CostPerUnit = 3.00m,
            IsActive = true
        };
        context.Ingredients.Add(ingredient);

        var inventoryItem = new InventoryItem
        {
            Id = Guid.NewGuid(),
            IngredientId = ingredient.Id,
            CurrentStock = 50.0m,
            AverageCost = 3.00m,
            Unit = "kg",
            LastUpdated = DateTime.UtcNow
        };
        context.InventoryItems.Add(inventoryItem);

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Test Category",
            IsActive = true,
            DisplayOrder = 1
        };
        context.Categories.Add(category);

        var menuItem1 = new MenuItem
        {
            Id = Guid.NewGuid(),
            Name = "Item 1",
            Price = 10.00m,
            CategoryId = category.Id,
            IsActive = true,
            HasIngredients = true
        };
        context.MenuItems.Add(menuItem1);

        var menuItem2 = new MenuItem
        {
            Id = Guid.NewGuid(),
            Name = "Item 2",
            Price = 12.00m,
            CategoryId = category.Id,
            IsActive = true,
            HasIngredients = true
        };
        context.MenuItems.Add(menuItem2);

        var recipe1 = new Recipe
        {
            Id = Guid.NewGuid(),
            MenuItemId = menuItem1.Id,
            Name = "Recipe 1",
            IsActive = true
        };
        context.Recipes.Add(recipe1);

        var recipe2 = new Recipe
        {
            Id = Guid.NewGuid(),
            MenuItemId = menuItem2.Id,
            Name = "Recipe 2",
            IsActive = true
        };
        context.Recipes.Add(recipe2);

        // Both recipes use the same ingredient
        var recipeLine1 = new RecipeLine
        {
            Id = Guid.NewGuid(),
            RecipeId = recipe1.Id,
            IngredientId = ingredient.Id,
            QuantityPerItem = 0.3m,
            Unit = "kg"
        };
        context.RecipeLines.Add(recipeLine1);

        var recipeLine2 = new RecipeLine
        {
            Id = Guid.NewGuid(),
            RecipeId = recipe2.Id,
            IngredientId = ingredient.Id,
            QuantityPerItem = 0.2m,
            Unit = "kg"
        };
        context.RecipeLines.Add(recipeLine2);

        await context.SaveChangesAsync();

        var createResponse = await _client.PostAsJsonAsync("/api/v1.0/orders", new
        {
            OrderType = OrderType.DineIn,
            CustomerId = (Guid?)null,
            OrderLines = new[]
            {
                new { MenuItemId = menuItem1.Id, Quantity = 2, UnitPrice = 10.00m, AddOns = new object[0] },
                new { MenuItemId = menuItem2.Id, Quantity = 3, UnitPrice = 12.00m, AddOns = new object[0] }
            },
            Notes = "Multi-line order"
        });
        var orderId = await ReadCreatedOrderIdAsync(createResponse);
        await TransitionOrderToOpenAsync(orderId);

        // Act - Finalize the order
        var finalizeRequest = new
        {
            Payments = new[]
            {
                new { PaymentMethod = "Cash", Amount = TotalWithLegacyTax(56.00m), TransactionId = "TXN-MULTI-001" }
            }
        };

        var finalizeResponse = await _client.PostAsJsonAsync($"/api/v1.0/orders/{orderId}/finalize", finalizeRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, finalizeResponse.StatusCode);

        var movements = await GetInventoryMovementsForOrderAsync(orderId);
        Assert.Single(movements);
        var movement = movements.First();
        Assert.Equal(-1.2m, movement.Quantity); // 0.6 + 0.6 = 1.2 kg total (aggregated)

        var updatedInventory = await GetInventoryItemByIngredientAsync(ingredient.Id);
        Assert.NotNull(updatedInventory);
        Assert.Equal(48.8m, updatedInventory.CurrentStock); // 50 - 1.2 = 48.8
    }

    [SkippableFact]
    public async Task FinalizeOrder_ShouldAllowNegativeStock_WhenDeductingBelowZero()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId();
        SetIdempotencyKey("test-negative-stock-001");

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PosDbContext>();

        // Create ingredient with low stock
        var ingredient = new Ingredient
        {
            Id = Guid.NewGuid(),
            Name = "Low Stock Ingredient",
            Unit = "kg",
            CostPerUnit = 2.00m,
            IsActive = true
        };
        context.Ingredients.Add(ingredient);

        var inventoryItem = new InventoryItem
        {
            Id = Guid.NewGuid(),
            IngredientId = ingredient.Id,
            CurrentStock = 1.0m, // Low stock
            AverageCost = 2.00m,
            Unit = "kg",
            LastUpdated = DateTime.UtcNow
        };
        context.InventoryItems.Add(inventoryItem);

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Test Category",
            IsActive = true,
            DisplayOrder = 1
        };
        context.Categories.Add(category);

        var menuItem = new MenuItem
        {
            Id = Guid.NewGuid(),
            Name = "Test Item",
            Price = 10.00m,
            CategoryId = category.Id,
            IsActive = true,
            HasIngredients = true
        };
        context.MenuItems.Add(menuItem);

        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            MenuItemId = menuItem.Id,
            Name = "Test Recipe",
            IsActive = true
        };
        context.Recipes.Add(recipe);

        var recipeLine = new RecipeLine
        {
            Id = Guid.NewGuid(),
            RecipeId = recipe.Id,
            IngredientId = ingredient.Id,
            QuantityPerItem = 2.0m, // Will deduct 2.0 kg (more than available)
            Unit = "kg"
        };
        context.RecipeLines.Add(recipeLine);

        await context.SaveChangesAsync();

        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1.0/orders",
            BuildCreateOrderPayload(menuItem.Id, 10.00m, notes: "Order that will cause negative stock"));
        var orderId = await ReadCreatedOrderIdAsync(createResponse);
        await TransitionOrderToOpenAsync(orderId);

        // Act - Finalize the order (should succeed even with negative stock)
        var finalizeRequest = new
        {
            Payments = new[]
            {
                new { PaymentMethod = "Cash", Amount = TotalWithLegacyTax(10.00m), TransactionId = "TXN-NEG-001" }
            }
        };

        var finalizeResponse = await _client.PostAsJsonAsync($"/api/v1.0/orders/{orderId}/finalize", finalizeRequest);

        // Assert - Should succeed (system allows negative stock)
        Assert.Equal(HttpStatusCode.OK, finalizeResponse.StatusCode);

        var updatedInventory = await GetInventoryItemByIngredientAsync(ingredient.Id);
        Assert.NotNull(updatedInventory);
        Assert.Equal(-1.0m, updatedInventory.CurrentStock); // 1.0 - 2.0 = -1.0 (negative allowed)

        var movements = await GetInventoryMovementsForOrderAsync(orderId);
        Assert.Single(movements);
    }

    [SkippableFact]
    public async Task FinalizeOrder_SecondFinalizeWithDifferentIdempotencyKey_WhenAlreadyPaid_DoesNotDuplicateInventoryMovements()
    {
        await InitializeDatabaseAsync();
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        SetDeviceId();

        Guid menuItemId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PosDbContext>();

            var ingredient = new Ingredient
            {
                Id = Guid.NewGuid(),
                Name = "DupFinalizeIngredient",
                Unit = "kg",
                CostPerUnit = 2m,
                IsActive = true
            };
            context.Ingredients.Add(ingredient);

            context.InventoryItems.Add(new InventoryItem
            {
                Id = Guid.NewGuid(),
                IngredientId = ingredient.Id,
                CurrentStock = 50m,
                MinimumStock = 0m,
                MaximumStock = 100m,
                AverageCost = 2m,
                Unit = "kg",
                LastUpdated = DateTime.UtcNow
            });

            var category = new Category
            {
                Id = Guid.NewGuid(),
                Name = "DupFinalizeCategory",
                IsActive = true,
                DisplayOrder = 1
            };
            context.Categories.Add(category);

            var menuItem = new MenuItem
            {
                Id = Guid.NewGuid(),
                Name = "DupFinalizeMenuItem",
                Price = 20m,
                CategoryId = category.Id,
                IsActive = true,
                HasIngredients = true
            };
            menuItemId = menuItem.Id;
            context.MenuItems.Add(menuItem);

            var recipe = new Recipe
            {
                Id = Guid.NewGuid(),
                MenuItemId = menuItem.Id,
                Name = "DupFinalizeRecipe",
                IsActive = true
            };
            context.Recipes.Add(recipe);

            context.RecipeLines.Add(new RecipeLine
            {
                Id = Guid.NewGuid(),
                RecipeId = recipe.Id,
                IngredientId = ingredient.Id,
                QuantityPerItem = 0.25m,
                Unit = "kg"
            });

            await context.SaveChangesAsync();
        }

        SetIdempotencyKey("dup-fin-1");
        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1.0/orders",
            BuildCreateOrderPayload(menuItemId, 20m, notes: "dup finalize governance"));
        var orderId = await ReadCreatedOrderIdAsync(createResponse);
        await TransitionOrderToOpenAsync(orderId);

        var finalizeRequest = new
        {
            Payments = new[]
            {
                new { PaymentMethod = "Cash", Amount = TotalWithLegacyTax(20m), TransactionId = "TXN-DUP-1" }
            }
        };

        var finalize1 = await _client.PostAsJsonAsync($"/api/v1.0/orders/{orderId}/finalize", finalizeRequest);
        finalize1.EnsureSuccessStatusCode();

        string orderNumber;
        int movementCount1;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PosDbContext>();
            orderNumber = await context.Orders.Where(o => o.Id == orderId).Select(o => o.OrderNumber).FirstAsync();
            movementCount1 = await context.InventoryMovements
                .CountAsync(im => im.Reference != null && im.Reference.Contains(orderNumber));
        }

        Assert.True(movementCount1 > 0, "Expected inventory movements after first finalize.");

        SetIdempotencyKey("dup-fin-2");
        var finalize2 = await _client.PostAsJsonAsync($"/api/v1.0/orders/{orderId}/finalize", finalizeRequest);
        finalize2.EnsureSuccessStatusCode();

        int movementCount2;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PosDbContext>();
            movementCount2 = await context.InventoryMovements
                .CountAsync(im => im.Reference != null && im.Reference.Contains(orderNumber));
        }

        Assert.Equal(movementCount1, movementCount2);
    }
}
