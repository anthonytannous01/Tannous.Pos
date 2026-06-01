using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Infrastructure.Repositories;

public class AdminPurgeRepository : IAdminPurgeRepository
{
    private readonly PosDbContext _db;

    public AdminPurgeRepository(PosDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Customer>> GetSoftDeletedCustomersAsync(
        DateTime cutoff, CancellationToken cancellationToken = default) =>
        await _db.Customers
            .Where(c => c.IsDeleted && c.DeletedAt < cutoff)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<MenuItem>> GetSoftDeletedMenuItemsAsync(
        DateTime cutoff, CancellationToken cancellationToken = default) =>
        await _db.MenuItems
            .Where(m => m.IsDeleted && m.DeletedAt < cutoff)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AddOn>> GetSoftDeletedAddOnsAsync(
        DateTime cutoff, CancellationToken cancellationToken = default) =>
        await _db.AddOns
            .Where(a => a.IsDeleted && a.DeletedAt < cutoff)
            .ToListAsync(cancellationToken);

    public async Task PurgeAsync(
        IReadOnlyList<Customer> customers,
        IReadOnlyList<MenuItem> menuItems,
        IReadOnlyList<AddOn> addOns,
        CancellationToken cancellationToken = default)
    {
        _db.Customers.RemoveRange(customers);
        _db.MenuItems.RemoveRange(menuItems);
        _db.AddOns.RemoveRange(addOns);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
