using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Application.DTOs.Catalog;
using Tannous.Pos.Application.DTOs.Customers;
using Tannous.Pos.Application.DTOs.Inventory;
using Tannous.Pos.Application.DTOs.Settings;
using Tannous.Pos.Application.DTOs.Sync;
using Tannous.Pos.Application.Sync;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Infrastructure.Services;

public class SyncPullService : ISyncPullService
{
    private readonly PosDbContext _context;

    public SyncPullService(PosDbContext context)
    {
        _context = context;
    }

    public async Task<PullResponseDto> PullAsync(
        DateTime sinceDate,
        int limit,
        int offset,
        CancellationToken cancellationToken)
    {
        var response = new PullResponseDto
        {
            Cursor   = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.ffffZ|v1"),
            Upserts  = new UpsertsDto(),
            Deletes  = new DeletesDto()
        };

        // Get settings (no date filter — always return current settings)
        var settings = await _context.BusinessSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
        if (settings != null)
        {
            response.Upserts.Settings = new List<SettingsDto>
            {
                new SettingsDto
                {
                    Id                       = settings.Id,
                    StoreName                = settings.BusinessName,
                    Address                  = settings.Address,
                    Phone                    = settings.Phone,
                    Email                    = settings.Email,
                    Website                  = settings.Website,
                    TaxNumber                = settings.TaxNumber,
                    TaxRate                  = settings.TaxRate,
                    Currency                 = settings.Currency,
                    TaxEnabled               = settings.TaxApplies,
                    ReceiptHeader            = settings.ReceiptHeader,
                    ReceiptFooter            = settings.ReceiptFooter,
                    RequireCustomerInfo      = settings.RequireCustomerInfo,
                    EnableInventoryTracking  = settings.EnableInventoryTracking,
                    EnableRecipeManagement   = settings.EnableRecipeManagement,
                    CreatedAt                = settings.CreatedAt,
                    UpdatedAt                = settings.UpdatedAt ?? DateTime.UtcNow
                }
            };
        }

        // Get categories
        var categories = await _context.Categories
            .AsNoTracking()
            .Where(c => c.UpdatedAt >= sinceDate)
            .ToListAsync(cancellationToken);
        if (categories.Any())
        {
            response.Upserts.Categories = categories.Select(c => new CategoryDto
            {
                Id           = c.Id,
                Name         = c.Name,
                Description  = c.Description,
                IsActive     = c.IsActive,
                DisplayOrder = c.DisplayOrder,
                CreatedAt    = c.CreatedAt
            }).ToList();
        }

        // Get menu items (paginated)
        var menuItems = await _context.MenuItems
            .AsNoTracking()
            .Include(m => m.Category)
            .Where(m => m.UpdatedAt >= sinceDate && !m.IsDeleted)
            .Skip(offset)
            .Take(limit + 1)
            .ToListAsync(cancellationToken);
        if (menuItems.Count > limit)
            menuItems.RemoveAt(limit);
        if (menuItems.Any())
        {
            response.Upserts.Items = menuItems.Select(m => new MenuItemDto
            {
                Id             = m.Id,
                Name           = m.Name,
                Description    = m.Description,
                Price          = m.Price,
                IsActive       = m.IsActive,
                ImageUrl       = m.ImageUrl,
                DisplayOrder   = m.DisplayOrder,
                HasAddOns      = m.HasAddOns,
                HasIngredients = m.HasIngredients,
                CategoryId     = m.CategoryId,
                CategoryName   = m.Category.Name,
                CreatedAt      = m.CreatedAt
            }).ToList();
        }

        // Get add-ons (paginated)
        var addOns = await _context.AddOns
            .AsNoTracking()
            .Where(a => a.UpdatedAt >= sinceDate && !a.IsDeleted)
            .Skip(offset)
            .Take(limit + 1)
            .ToListAsync(cancellationToken);
        if (addOns.Count > limit)
            addOns.RemoveAt(limit);
        if (addOns.Any())
        {
            response.Upserts.AddOns = addOns.Select(a => new AddOnDto
            {
                Id          = a.Id,
                Name        = a.Name,
                Description = a.Description,
                Price       = a.Price,
                IsActive    = a.IsActive,
                CreatedAt   = a.CreatedAt
            }).ToList();
        }

        // Get customers (paginated)
        var customers = await _context.Customers
            .AsNoTracking()
            .Where(c => c.UpdatedAt >= sinceDate && !c.IsDeleted)
            .Skip(offset)
            .Take(limit + 1)
            .ToListAsync(cancellationToken);
        if (customers.Count > limit)
            customers.RemoveAt(limit);
        if (customers.Any())
        {
            response.Upserts.Customers = customers.Select(c => new CustomerDto
            {
                Id            = c.Id,
                FirstName     = c.FirstName,
                LastName      = c.LastName,
                Email         = c.Email,
                Phone         = c.Phone,
                Address       = c.Address,
                Notes         = c.Notes,
                Allergies     = c.Allergies,
                IsActive      = c.IsActive,
                LastVisitDate = c.LastVisitDate,
                TotalOrders   = c.TotalOrders,
                CreatedAt     = c.CreatedAt
            }).ToList();
        }

        // Get ingredients (no pagination)
        var ingredients = await _context.Ingredients
            .AsNoTracking()
            .Where(i => i.UpdatedAt >= sinceDate)
            .ToListAsync(cancellationToken);
        if (ingredients.Any())
        {
            response.Upserts.Ingredients = ingredients.Select(i => new IngredientDto
            {
                Id          = i.Id,
                Name        = i.Name,
                Description = i.Description,
                CostPerUnit = i.CostPerUnit,
                Unit        = i.Unit,
                IsActive    = i.IsActive,
                CreatedAt   = i.CreatedAt
            }).ToList();
        }

        // Get recipes (no pagination, include RecipeLines + Ingredient; NOT MenuItem)
        var recipes = await _context.Recipes
            .AsNoTracking()
            .Include(r => r.RecipeLines)
                .ThenInclude(rl => rl.Ingredient)
            .Where(r => r.UpdatedAt >= sinceDate)
            .ToListAsync(cancellationToken);
        if (recipes.Any())
        {
            response.Upserts.Recipes = recipes.Select(r => new RecipeDto
            {
                Id          = r.Id,
                Name        = r.Name,
                Description = r.Description,
                MenuItemId  = r.MenuItemId,
                IsActive    = r.IsActive,
                RecipeLines = r.RecipeLines.Select(rl => new RecipeLineDto
                {
                    Id              = rl.Id,
                    IngredientId    = rl.IngredientId,
                    IngredientName  = rl.Ingredient.Name,
                    QuantityPerItem = rl.QuantityPerItem,
                    Unit            = rl.Unit               // RecipeLine.Unit — NOT rl.Ingredient.Unit
                }).ToList(),
                CreatedAt   = r.CreatedAt
            }).ToList();
        }

        // HasMore tracks NextToken: false while server-side pagination tokens are not yet issued.
        response.HasMore = response.NextToken != null;

        return response;
    }
}
