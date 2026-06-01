using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Domain.Interfaces;

public interface IRecipeRepository : IRepository<Recipe>
{
    Task<Recipe?> GetByIdWithDetailsAsync(Guid id);
    Task<IEnumerable<Recipe>> GetActiveRecipesAsync();
    Task<IEnumerable<Recipe>> GetByMenuItemAsync(Guid menuItemId);
    Task<IEnumerable<Recipe>> GetByIngredientAsync(Guid ingredientId);
}
