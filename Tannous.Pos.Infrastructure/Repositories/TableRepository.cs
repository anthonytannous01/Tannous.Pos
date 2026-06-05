using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Infrastructure.Repositories;

public class TableRepository : ITableRepository
{
    private readonly PosDbContext _db;

    public TableRepository(PosDbContext db) => _db = db;

    public async Task<IEnumerable<Table>> GetActiveAsync(
        int minCapacity = 1, CancellationToken ct = default)
        => await _db.Tables
            .Include(t => t.FloorPlan)
            .AsNoTracking()
            .Where(t => t.IsActive && t.Capacity >= minCapacity)
            .OrderBy(t => t.FloorPlan.DisplayOrder)
            .ThenBy(t => t.DisplayOrder)
            .ToListAsync(ct);
}
