using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Domain.Interfaces;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Infrastructure.Repositories;

public class ShiftRepository : Repository<Shift>, IShiftRepository
{
    public ShiftRepository(PosDbContext context) : base(context)
    {
    }

    public async Task<Shift?> GetByIdWithDetailsAsync(Guid id)
    {
        return await _dbSet
            .Include(s => s.User)
            .Include(s => s.Orders)
                .ThenInclude(o => o.Payments)
            .Include(s => s.CashDrawerEvents)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Shift?> GetOpenShiftByUserAsync(Guid userId)
    {
        return await _dbSet
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == ShiftStatus.Open);
    }

    public async Task<IEnumerable<Shift>> GetByStatusAsync(ShiftStatus status)
    {
        return await _dbSet
            .Include(s => s.User)
            .Where(s => s.Status == status)
            .OrderByDescending(s => s.StartTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Shift>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Include(s => s.User)
            .Where(s => s.StartTime >= startDate && s.StartTime <= endDate)
            .OrderByDescending(s => s.StartTime)
            .ToListAsync();
    }

    public async Task<Shift?> GetByShiftNumberAsync(string shiftNumber)
    {
        return await _dbSet
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.ShiftNumber == shiftNumber);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
