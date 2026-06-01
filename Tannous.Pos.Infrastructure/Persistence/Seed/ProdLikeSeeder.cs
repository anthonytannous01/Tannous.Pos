using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Infrastructure.Persistence.Seed;

public class ProdLikeSeeder
{
    private readonly PosDbContext _context;
    private readonly ILogger<ProdLikeSeeder> _logger;

    public ProdLikeSeeder(PosDbContext context, ILogger<ProdLikeSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        _logger.LogInformation("Starting production-like data seeding...");

        try
        {
            // Business Settings
            await SeedBusinessSettingsAsync();

            // Categories
            await SeedCategoriesAsync();

            // Ingredients
            await SeedIngredientsAsync();

            // Menu Items
            await SeedMenuItemsAsync();

            // Add-ons
            await SeedAddOnsAsync();

            // Recipes
            await SeedRecipesAsync();

            // Inventory Items
            await SeedInventoryItemsAsync();

            // Customers
            await SeedCustomersAsync();

            // Devices
            await SeedDevicesAsync();

            await _context.SaveChangesAsync();
            _logger.LogInformation("✅ Production-like data seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during production-like data seeding");
            throw;
        }
    }

    private async Task SeedBusinessSettingsAsync()
    {
        if (await _context.BusinessSettings.AnyAsync())
        {
            _logger.LogInformation("Business settings already exist, skipping...");
            return;
        }

        var settings = new BusinessSettings
        {
            BusinessName = "Tannous",
            Address = "123 Main Street, Beirut, Lebanon",
            Phone = "+961 1 234 567",
            Email = "info@tannous.com",
            Currency = "LBP",
            TaxRate = 0.11m, // 11% VAT
            ReceiptFooter = "Thank you for choosing Tannous!"
        };

        _context.BusinessSettings.Add(settings);
        _logger.LogInformation("✅ Created business settings");
    }

    private async Task SeedCategoriesAsync()
    {
        if (await _context.Categories.AnyAsync())
        {
            _logger.LogInformation("Categories already exist, skipping...");
            return;
        }

        var categories = new[]
        {
            new Category { Name = "Manakish", Description = "Traditional Lebanese flatbread", IsActive = true },
            new Category { Name = "Fast Food", Description = "Quick meals and sandwiches", IsActive = true },
            new Category { Name = "Drinks", Description = "Beverages and refreshments", IsActive = true },
            new Category { Name = "Desserts", Description = "Sweet treats and pastries", IsActive = true }
        };

        _context.Categories.AddRange(categories);
        _logger.LogInformation("✅ Created {Count} categories", categories.Length);
    }

    private async Task SeedIngredientsAsync()
    {
        if (await _context.Ingredients.AnyAsync())
        {
            _logger.LogInformation("Ingredients already exist, skipping...");
            return;
        }

        var ingredients = new[]
        {
            new Ingredient { Name = "Flour", Unit = "kg", CostPerUnit = 2.50m, IsActive = true },
            new Ingredient { Name = "Cheese", Unit = "kg", CostPerUnit = 15.00m, IsActive = true },
            new Ingredient { Name = "Zaatar", Unit = "kg", CostPerUnit = 25.00m, IsActive = true },
            new Ingredient { Name = "Olive Oil", Unit = "L", CostPerUnit = 8.00m, IsActive = true },
            new Ingredient { Name = "Tomato Sauce", Unit = "L", CostPerUnit = 3.50m, IsActive = true },
            new Ingredient { Name = "Soft Drink Syrup", Unit = "L", CostPerUnit = 5.00m, IsActive = true }
        };

        _context.Ingredients.AddRange(ingredients);
        _logger.LogInformation("✅ Created {Count} ingredients", ingredients.Length);
    }

    private async Task SeedMenuItemsAsync()
    {
        if (await _context.MenuItems.AnyAsync())
        {
            _logger.LogInformation("Menu items already exist, skipping...");
            return;
        }

        var categories = await _context.Categories.ToListAsync();
        var manakishCategory = categories.FirstOrDefault(c => c.Name == "Manakish");
        var fastFoodCategory = categories.FirstOrDefault(c => c.Name == "Fast Food");
        var drinksCategory = categories.FirstOrDefault(c => c.Name == "Drinks");

        var menuItems = new List<MenuItem>();

        if (manakishCategory != null)
        {
            menuItems.AddRange(new[]
            {
                new MenuItem 
                { 
                    Name = "Zaatar Manakish", 
                    Description = "Traditional zaatar flatbread",
                    Price = 3.50m,
                    CategoryId = manakishCategory.Id,
                    IsActive = true
                },
                new MenuItem 
                { 
                    Name = "Cheese Manakish", 
                    Description = "Fresh cheese flatbread",
                    Price = 4.00m,
                    CategoryId = manakishCategory.Id,
                    IsActive = true
                }
            });
        }

        if (fastFoodCategory != null)
        {
            menuItems.Add(new MenuItem 
            { 
                Name = "Chicken Shawarma", 
                Description = "Grilled chicken with vegetables",
                Price = 8.50m,
                CategoryId = fastFoodCategory.Id,
                IsActive = true
            });
        }

        if (drinksCategory != null)
        {
            menuItems.Add(new MenuItem 
            { 
                Name = "Soft Drink", 
                Description = "Carbonated beverage",
                Price = 2.00m,
                CategoryId = drinksCategory.Id,
                IsActive = true
            });
        }

        _context.MenuItems.AddRange(menuItems);
        _logger.LogInformation("✅ Created {Count} menu items", menuItems.Count);
    }

    private async Task SeedAddOnsAsync()
    {
        if (await _context.AddOns.AnyAsync())
        {
            _logger.LogInformation("Add-ons already exist, skipping...");
            return;
        }

        var addOns = new[]
        {
            new AddOn { Name = "Extra Cheese", Price = 1.50m, IsActive = true },
            new AddOn { Name = "Extra Zaatar", Price = 0.75m, IsActive = true },
            new AddOn { Name = "Extra Sauce", Price = 0.50m, IsActive = true }
        };

        _context.AddOns.AddRange(addOns);
        _logger.LogInformation("✅ Created {Count} add-ons", addOns.Length);
    }

    private async Task SeedRecipesAsync()
    {
        if (await _context.Recipes.AnyAsync())
        {
            _logger.LogInformation("Recipes already exist, skipping...");
            return;
        }

        var menuItems = await _context.MenuItems.ToListAsync();
        var ingredients = await _context.Ingredients.ToListAsync();

        var zaatarManakish = menuItems.FirstOrDefault(m => m.Name == "Zaatar Manakish");
        var cheeseManakish = menuItems.FirstOrDefault(m => m.Name == "Cheese Manakish");
        var softDrink = menuItems.FirstOrDefault(m => m.Name == "Soft Drink");

        var flour = ingredients.FirstOrDefault(i => i.Name == "Flour");
        var zaatar = ingredients.FirstOrDefault(i => i.Name == "Zaatar");
        var cheese = ingredients.FirstOrDefault(i => i.Name == "Cheese");
        var oliveOil = ingredients.FirstOrDefault(i => i.Name == "Olive Oil");
        var softDrinkSyrup = ingredients.FirstOrDefault(i => i.Name == "Soft Drink Syrup");

        var recipes = new List<Recipe>();

        if (zaatarManakish != null && flour != null && zaatar != null && oliveOil != null)
        {
            var zaatarRecipe = new Recipe
            {
                MenuItemId = zaatarManakish.Id,
                Name = "Zaatar Manakish Recipe",
                IsActive = true,
                RecipeLines = new List<RecipeLine>
                {
                    new RecipeLine { IngredientId = flour.Id, QuantityPerItem = 0.15m }, // 150g flour
                    new RecipeLine { IngredientId = zaatar.Id, QuantityPerItem = 0.02m }, // 20g zaatar
                    new RecipeLine { IngredientId = oliveOil.Id, QuantityPerItem = 0.01m } // 10ml oil
                }
            };
            recipes.Add(zaatarRecipe);
        }

        if (cheeseManakish != null && flour != null && cheese != null)
        {
            var cheeseRecipe = new Recipe
            {
                MenuItemId = cheeseManakish.Id,
                Name = "Cheese Manakish Recipe",
                IsActive = true,
                RecipeLines = new List<RecipeLine>
                {
                    new RecipeLine { IngredientId = flour.Id, QuantityPerItem = 0.15m }, // 150g flour
                    new RecipeLine { IngredientId = cheese.Id, QuantityPerItem = 0.08m } // 80g cheese
                }
            };
            recipes.Add(cheeseRecipe);
        }

        if (softDrink != null && softDrinkSyrup != null)
        {
            var softDrinkRecipe = new Recipe
            {
                MenuItemId = softDrink.Id,
                Name = "Soft Drink Recipe",
                IsActive = true,
                RecipeLines = new List<RecipeLine>
                {
                    new RecipeLine { IngredientId = softDrinkSyrup.Id, QuantityPerItem = 0.05m } // 50ml syrup
                }
            };
            recipes.Add(softDrinkRecipe);
        }

        _context.Recipes.AddRange(recipes);
        _logger.LogInformation("✅ Created {Count} recipes", recipes.Count);
    }

    private async Task SeedInventoryItemsAsync()
    {
        if (await _context.InventoryItems.AnyAsync())
        {
            _logger.LogInformation("Inventory items already exist, skipping...");
            return;
        }

        var ingredients = await _context.Ingredients.ToListAsync();

        var inventoryItems = new List<InventoryItem>();

        foreach (var ingredient in ingredients)
        {
            var initialStock = ingredient.Name switch
            {
                "Flour" => 50.0m, // 50kg
                "Cheese" => 20.0m, // 20kg
                "Zaatar" => 5.0m,  // 5kg
                "Olive Oil" => 10.0m, // 10L
                "Tomato Sauce" => 15.0m, // 15L
                "Soft Drink Syrup" => 8.0m, // 8L
                _ => 10.0m
            };

            inventoryItems.Add(new InventoryItem
            {
                IngredientId = ingredient.Id,
                CurrentStock = initialStock,
                Unit = ingredient.Unit,
                MinimumStock = initialStock * 0.2m, // 20% of initial stock
                MaximumStock = initialStock * 2.0m, // 200% of initial stock
                LastUpdated = DateTime.UtcNow
            });
        }

        _context.InventoryItems.AddRange(inventoryItems);
        _logger.LogInformation("✅ Created {Count} inventory items", inventoryItems.Count);
    }

    private async Task SeedCustomersAsync()
    {
        if (await _context.Customers.AnyAsync())
        {
            _logger.LogInformation("Customers already exist, skipping...");
            return;
        }

        var customer = new Customer
        {
            FirstName = "Test",
            LastName = "Customer",
            Phone = "+961 70 123 456",
            Email = "test@example.com",
            IsActive = true
        };

        _context.Customers.Add(customer);
        _logger.LogInformation("✅ Created test customer");
    }

    private async Task SeedDevicesAsync()
    {
        if (await _context.Devices.AnyAsync())
        {
            _logger.LogInformation("Devices already exist, skipping...");
            return;
        }

        var device = new Device
        {
            DeviceId = "Front-Register-001",
            Name = "Front Register",
            Location = "Main Counter",
            IsActive = true
        };

        _context.Devices.Add(device);
        _logger.LogInformation("✅ Created front register device");
    }
}
