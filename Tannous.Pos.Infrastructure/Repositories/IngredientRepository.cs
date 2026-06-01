using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Infrastructure.Repositories;

public class IngredientRepository : Repository<Ingredient>, IIngredientRepository
{
    public IngredientRepository(PosDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Ingredient>> GetActiveIngredientsAsync()
    {
        return await _dbSet
            .Where(i => i.IsActive)
            .OrderBy(i => i.Name)
            .ToListAsync();
    }

    public async Task<Ingredient?> GetByNameAsync(string name)
    {
        return await _dbSet
            .FirstOrDefaultAsync(i => i.Name == name);
    }
}
