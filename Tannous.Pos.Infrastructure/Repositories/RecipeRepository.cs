using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Infrastructure.Repositories;

public class RecipeRepository : Repository<Recipe>, IRecipeRepository
{
    public RecipeRepository(PosDbContext context) : base(context)
    {
    }

    public async Task<Recipe?> GetByIdWithDetailsAsync(Guid id)
    {
        return await _dbSet
            .Include(r => r.MenuItem)
            .Include(r => r.RecipeLines)
                .ThenInclude(rl => rl.Ingredient)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IEnumerable<Recipe>> GetActiveRecipesAsync()
    {
        return await _dbSet
            .Include(r => r.MenuItem)
            .Include(r => r.RecipeLines)
                .ThenInclude(rl => rl.Ingredient)
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Recipe>> GetByMenuItemAsync(Guid menuItemId)
    {
        return await _dbSet
            .Include(r => r.MenuItem)
            .Include(r => r.RecipeLines)
                .ThenInclude(rl => rl.Ingredient)
            .Where(r => r.MenuItemId == menuItemId && r.IsActive)
            .OrderBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Recipe>> GetByIngredientAsync(Guid ingredientId)
    {
        return await _dbSet
            .Include(r => r.MenuItem)
            .Include(r => r.RecipeLines)
                .ThenInclude(rl => rl.Ingredient)
            .Where(r => r.RecipeLines.Any(rl => rl.IngredientId == ingredientId))
            .OrderBy(r => r.Name)
            .ToListAsync();
    }
}
