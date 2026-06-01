using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Domain.Interfaces;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Infrastructure.Repositories;

public class OrderRepository : Repository<Order>, IOrderRepository
{
    public OrderRepository(PosDbContext context) : base(context)
    {
    }

    public async Task<Order?> GetByIdWithDetailsAsync(Guid id)
    {
        return await _dbSet
            .AsSplitQuery()
            .Include(o => o.User)
            .Include(o => o.Customer)
            .Include(o => o.Shift)
            .Include(o => o.OrderLines)
                .ThenInclude(ol => ol.MenuItem)
            .Include(o => o.OrderLines)
                .ThenInclude(ol => ol.OrderLineAddOns)
                    .ThenInclude(ola => ola.AddOn)
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.Customer)
            .Include(o => o.Shift)
            .Where(o => o.Status == status)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Order>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.Customer)
            .Include(o => o.Shift)
            .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<Order?> GetByOrderNumberAsync(string orderNumber)
    {
        return await _dbSet
            .AsNoTracking()
            .AsSplitQuery()
            .Include(o => o.User)
            .Include(o => o.Customer)
            .Include(o => o.Shift)
            .Include(o => o.OrderLines)
                .ThenInclude(ol => ol.MenuItem)
            .Include(o => o.OrderLines)
                .ThenInclude(ol => ol.OrderLineAddOns)
                    .ThenInclude(ola => ola.AddOn)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
    }

    public async Task<IEnumerable<Order>> GetByCustomerAsync(Guid customerId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.Customer)
            .Include(o => o.Shift)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Order>> GetByShiftAsync(Guid shiftId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.Customer)
            .Include(o => o.Shift)
            .Where(o => o.ShiftId == shiftId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Order>> GetPaidOrdersInDateRangeAsync(DateTime from, DateTime to)
    {
        return await _dbSet
            .AsNoTracking()
            .AsSplitQuery()
            .Include(o => o.OrderLines)
                .ThenInclude(ol => ol.MenuItem)
                    .ThenInclude(mi => mi.Recipes)
                        .ThenInclude(r => r.RecipeLines)
                            .ThenInclude(rl => rl.Ingredient)
            .Where(o => o.Status == OrderStatus.Paid && o.CreatedAt >= from && o.CreatedAt <= to)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }
}
