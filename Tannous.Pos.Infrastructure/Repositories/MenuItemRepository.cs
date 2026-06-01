using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Infrastructure.Repositories;

public class MenuItemRepository : Repository<MenuItem>, IMenuItemRepository
{
    public MenuItemRepository(PosDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<MenuItem>> GetActiveMenuItemsAsync()
    {
        return await _dbSet
            .Include(m => m.Category)
            .Where(m => m.IsActive)
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync();
    }

    public async Task<IEnumerable<MenuItem>> GetByCategoryAsync(Guid categoryId)
    {
        return await _dbSet
            .Include(m => m.Category)
            .Where(m => m.CategoryId == categoryId && m.IsActive)
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync();
    }

    public async Task<MenuItem?> GetByNameAsync(string name)
    {
        return await _dbSet
            .Include(m => m.Category)
            .FirstOrDefaultAsync(m => m.Name == name);
    }

    public async Task<MenuItem?> GetByIdWithCategoryAsync(Guid id)
    {
        return await _dbSet
            .Include(m => m.Category)
            .FirstOrDefaultAsync(m => m.Id == id);
    }
}
