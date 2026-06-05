using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Infrastructure.Repositories;

public class BranchRepository : IBranchRepository
{
    private readonly PosDbContext _db;

    public BranchRepository(PosDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Branch>> GetAllAsync(
        bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        var query = _db.Branches.AsNoTracking();
        if (activeOnly) query = query.Where(b => b.IsActive);
        return await query
            .OrderBy(b => b.DisplayOrder)
            .ThenBy(b => b.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Branch?> GetDefaultAsync(CancellationToken cancellationToken = default) =>
        _db.Branches.AsNoTracking()
            .Where(b => b.IsDefault && b.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<Branch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Branches.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task AddAsync(Branch branch, CancellationToken cancellationToken = default)
    {
        await _db.Branches.AddAsync(branch, cancellationToken);
    }

    public async Task ClearDefaultAsync(CancellationToken cancellationToken = default)
    {
        await _db.Branches
            .Where(b => b.IsDefault)
            .ExecuteUpdateAsync(
                s => s.SetProperty(b => b.IsDefault, false),
                cancellationToken);
    }
}
