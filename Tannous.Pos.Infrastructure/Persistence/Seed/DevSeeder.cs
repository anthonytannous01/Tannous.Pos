using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Infrastructure.Persistence.Seed;

public class DevSeeder
{
    private readonly PosDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DevSeeder> _logger;

    public DevSeeder(PosDbContext context, IConfiguration configuration, ILogger<DevSeeder> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Seeds the first Owner (Admin) user from environment variables.
    /// Only runs if SEED_ADMIN_* environment variables are set.
    /// </summary>
    public async Task SeedAdminUserAsync()
    {
        // Read environment variables (with fallback to config for testing)
        var username = Environment.GetEnvironmentVariable("SEED_ADMIN_USERNAME") 
                      ?? Environment.GetEnvironmentVariable("SEED_OWNER_USERNAME")
                      ?? _configuration["Seed:Admin:Username"];
        
        var password = Environment.GetEnvironmentVariable("SEED_ADMIN_PASSWORD")
                      ?? _configuration["Seed:Admin:Password"];
        
        var firstName = Environment.GetEnvironmentVariable("SEED_ADMIN_FIRSTNAME")
                       ?? _configuration["Seed:Admin:FirstName"];
        
        var lastName = Environment.GetEnvironmentVariable("SEED_ADMIN_LASTNAME")
                      ?? _configuration["Seed:Admin:LastName"];
        
        var email = Environment.GetEnvironmentVariable("SEED_ADMIN_EMAIL")
                   ?? _configuration["Seed:Admin:Email"];

        // Check if required env vars are present
        if (string.IsNullOrWhiteSpace(username) || 
            string.IsNullOrWhiteSpace(password) || 
            string.IsNullOrWhiteSpace(firstName) || 
            string.IsNullOrWhiteSpace(lastName))
        {
            _logger.LogInformation("Admin user seeding skipped: Required environment variables (SEED_ADMIN_USERNAME, SEED_ADMIN_PASSWORD, SEED_ADMIN_FIRSTNAME, SEED_ADMIN_LASTNAME) are not set.");
            return;
        }

        // Normalize username for duplicate check
        var normalizedUsername = username.ToUpperInvariant();
        
        // Check if Owner user already exists (idempotent)
        var existingOwner = await _context.Users
            .FirstOrDefaultAsync(u => u.NormalizedUsername == normalizedUsername);
        
        if (existingOwner != null)
        {
            _logger.LogInformation("Admin user seeding skipped: Owner user with username '{Username}' already exists.", username);
            return;
        }

        // Normalize email (null if empty)
        string? normalizedEmail = null;
        if (!string.IsNullOrWhiteSpace(email))
        {
            normalizedEmail = email.ToUpperInvariant();
            
            // Check if email is already taken
            var existingEmail = await _context.Users
                .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);
            
            if (existingEmail != null)
            {
                _logger.LogWarning("Admin user seeding skipped: Email '{Email}' is already registered to another user.", email);
                return;
            }
        }

        // Hash password using BCrypt
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        // Create Owner user with all required fields
        var owner = new User
        {
            Username = username,
            NormalizedUsername = normalizedUsername,
            Email = email ?? string.Empty,
            NormalizedEmail = normalizedEmail,
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            Role = Role.Owner,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(owner);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Admin user seeded successfully: Username '{Username}', Email '{Email}'", username, email ?? "none");
    }

    public async Task SeedAsync()
    {
        if (await _context.Users.AnyAsync())
            return; // Already seeded

        // Create users
        var owner = new User
        {
            Username = "owner",
            Email = "owner@tannous.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = Role.Owner,
            IsActive = true
        };

        var cashier = new User
        {
            Username = "cashier",
            Email = "cashier@tannous.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = Role.Cashier,
            IsActive = true
        };

        _context.Users.AddRange(owner, cashier);

        // Create business settings
        var settings = new BusinessSettings
        {
            BusinessName = "Tannous Restaurant",
            Address = "123 Main St, City, State 12345",
            Phone = "(555) 123-4567",
            Email = "info@tannous.com",
            Website = "https://tannous.com",
            TaxNumber = "TAX123456",
            TaxRate = 8.5m,
            Currency = "USD",
            ReceiptHeader = "Thank you for dining with us!",
            ReceiptFooter = "Please come again",
            RequireCustomerInfo = false,
            EnableInventoryTracking = true,
            EnableRecipeManagement = true
        };

        _context.BusinessSettings.Add(settings);

        // Create categories
        var categories = new List<Category>
        {
            new() { Name = "Appetizers", Description = "Start your meal right", DisplayOrder = 1, IsActive = true },
            new() { Name = "Main Courses", Description = "Delicious main dishes", DisplayOrder = 2, IsActive = true },
            new() { Name = "Desserts", Description = "Sweet endings", DisplayOrder = 3, IsActive = true }
        };

        _context.Categories.AddRange(categories);
        await _context.SaveChangesAsync();

        // Create menu items
        var menuItems = new List<MenuItem>
        {
            new() { Name = "Hummus", Description = "Creamy chickpea dip", Price = 8.99m, CategoryId = categories[0].Id, DisplayOrder = 1, IsActive = true, HasAddOns = true },
            new() { Name = "Falafel", Description = "Crispy chickpea fritters", Price = 12.99m, CategoryId = categories[0].Id, DisplayOrder = 2, IsActive = true, HasAddOns = true },
            new() { Name = "Shawarma", Description = "Grilled chicken wrap", Price = 15.99m, CategoryId = categories[1].Id, DisplayOrder = 1, IsActive = true, HasAddOns = true, HasIngredients = true },
            new() { Name = "Kebab", Description = "Grilled meat skewers", Price = 18.99m, CategoryId = categories[1].Id, DisplayOrder = 2, IsActive = true, HasAddOns = true, HasIngredients = true },
            new() { Name = "Baklava", Description = "Sweet pastry dessert", Price = 6.99m, CategoryId = categories[2].Id, DisplayOrder = 1, IsActive = true },
            new() { Name = "Kunafa", Description = "Cheese pastry dessert", Price = 7.99m, CategoryId = categories[2].Id, DisplayOrder = 2, IsActive = true }
        };

        _context.MenuItems.AddRange(menuItems);

        // Create add-ons
        var addOns = new List<AddOn>
        {
            new() { Name = "Extra Sauce", Description = "Additional sauce", Price = 1.50m, DisplayOrder = 1, IsActive = true },
            new() { Name = "Extra Meat", Description = "Additional meat portion", Price = 3.00m, DisplayOrder = 2, IsActive = true },
            new() { Name = "Cheese", Description = "Add cheese", Price = 2.00m, DisplayOrder = 3, IsActive = true },
            new() { Name = "Vegetables", Description = "Extra vegetables", Price = 1.00m, DisplayOrder = 4, IsActive = true }
        };

        _context.AddOns.AddRange(addOns);

        // Create ingredients
        var ingredients = new List<Ingredient>
        {
            new() { Name = "Chickpeas", Description = "Dried chickpeas", CostPerUnit = 2.50m, Unit = "kg", IsActive = true },
            new() { Name = "Chicken Breast", Description = "Fresh chicken breast", CostPerUnit = 8.00m, Unit = "kg", IsActive = true },
            new() { Name = "Beef", Description = "Ground beef", CostPerUnit = 12.00m, Unit = "kg", IsActive = true },
            new() { Name = "Flour", Description = "All-purpose flour", CostPerUnit = 1.20m, Unit = "kg", IsActive = true },
            new() { Name = "Olive Oil", Description = "Extra virgin olive oil", CostPerUnit = 15.00m, Unit = "L", IsActive = true }
        };

        _context.Ingredients.AddRange(ingredients);
        await _context.SaveChangesAsync();

        // Create inventory items
        var inventoryItems = new List<InventoryItem>
        {
            new() { IngredientId = ingredients[0].Id, CurrentStock = 50.0m, MinimumStock = 10.0m, MaximumStock = 100.0m, AverageCost = 2.50m, LastUpdated = DateTime.UtcNow },
            new() { IngredientId = ingredients[1].Id, CurrentStock = 25.0m, MinimumStock = 5.0m, MaximumStock = 50.0m, AverageCost = 8.00m, LastUpdated = DateTime.UtcNow },
            new() { IngredientId = ingredients[2].Id, CurrentStock = 20.0m, MinimumStock = 5.0m, MaximumStock = 40.0m, AverageCost = 12.00m, LastUpdated = DateTime.UtcNow },
            new() { IngredientId = ingredients[3].Id, CurrentStock = 30.0m, MinimumStock = 5.0m, MaximumStock = 60.0m, AverageCost = 1.20m, LastUpdated = DateTime.UtcNow },
            new() { IngredientId = ingredients[4].Id, CurrentStock = 10.0m, MinimumStock = 2.0m, MaximumStock = 20.0m, AverageCost = 15.00m, LastUpdated = DateTime.UtcNow }
        };

        _context.InventoryItems.AddRange(inventoryItems);

        // Create recipes
        var recipes = new List<Recipe>
        {
            new() { Name = "Hummus Recipe", Description = "Traditional hummus", MenuItemId = menuItems[0].Id, IsActive = true },
            new() { Name = "Falafel Recipe", Description = "Crispy falafel", MenuItemId = menuItems[1].Id, IsActive = true },
            new() { Name = "Shawarma Recipe", Description = "Chicken shawarma", MenuItemId = menuItems[2].Id, IsActive = true }
        };

        _context.Recipes.AddRange(recipes);
        await _context.SaveChangesAsync();

        // Create recipe lines
        var recipeLines = new List<RecipeLine>
        {
            new() { RecipeId = recipes[0].Id, IngredientId = ingredients[0].Id, QuantityPerItem = 0.1m, Unit = "kg" },
            new() { RecipeId = recipes[0].Id, IngredientId = ingredients[4].Id, QuantityPerItem = 0.02m, Unit = "L" },
            new() { RecipeId = recipes[1].Id, IngredientId = ingredients[0].Id, QuantityPerItem = 0.15m, Unit = "kg" },
            new() { RecipeId = recipes[1].Id, IngredientId = ingredients[4].Id, QuantityPerItem = 0.03m, Unit = "L" },
            new() { RecipeId = recipes[2].Id, IngredientId = ingredients[1].Id, QuantityPerItem = 0.2m, Unit = "kg" },
            new() { RecipeId = recipes[2].Id, IngredientId = ingredients[4].Id, QuantityPerItem = 0.01m, Unit = "L" }
        };

        _context.RecipeLines.AddRange(recipeLines);

        // Create customers
        var customers = new List<Customer>
        {
            new() { FirstName = "John", LastName = "Doe", Email = "john@example.com", Phone = "(555) 111-1111", IsActive = true },
            new() { FirstName = "Jane", LastName = "Smith", Email = "jane@example.com", Phone = "(555) 222-2222", IsActive = true },
            new() { FirstName = "Bob", LastName = "Johnson", Email = "bob@example.com", Phone = "(555) 333-3333", IsActive = true }
        };

        _context.Customers.AddRange(customers);

        // Create devices
        var devices = new List<Device>
        {
            new() { DeviceId = "POS-001", Name = "Main POS Terminal", DeviceType = "Terminal", IsActive = true },
            new() { DeviceId = "POS-002", Name = "Kitchen Display", DeviceType = "Display", IsActive = true }
        };

        _context.Devices.AddRange(devices);

        await _context.SaveChangesAsync();
    }
}
