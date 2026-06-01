using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Infrastructure.Repositories;

public class AddOnRepository : Repository<AddOn>, IAddOnRepository
{
    public AddOnRepository(PosDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<AddOn>> GetActiveAddOnsAsync()
    {
        return await _dbSet
            .Where(a => a.IsActive)
            .OrderBy(a => a.DisplayOrder)
            .ThenBy(a => a.Name)
            .ToListAsync();
    }

    public async Task<AddOn?> GetByNameAsync(string name)
    {
        return await _dbSet
            .FirstOrDefaultAsync(a => a.Name == name);
    }
}
