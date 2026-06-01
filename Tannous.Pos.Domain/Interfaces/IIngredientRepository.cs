using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Domain.Interfaces;

public interface IIngredientRepository : IRepository<Ingredient>
{
    Task<IEnumerable<Ingredient>> GetActiveIngredientsAsync();
    Task<Ingredient?> GetByNameAsync(string name);
}
