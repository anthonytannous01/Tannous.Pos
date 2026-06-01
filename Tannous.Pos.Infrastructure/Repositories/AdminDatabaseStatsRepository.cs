using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Application.DTOs.Admin;
using Tannous.Pos.Application.Interfaces;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Infrastructure.Repositories;

public class AdminDatabaseStatsRepository : IAdminDatabaseStatsRepository
{
    private readonly PosDbContext _db;

    public AdminDatabaseStatsRepository(PosDbContext db)
    {
        _db = db;
    }

    public async Task<AdminDatabaseStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var orders          = await _db.Orders.CountAsync(cancellationToken);
        var customers       = await _db.Customers.CountAsync(cancellationToken);
        var menuItems       = await _db.MenuItems.CountAsync(cancellationToken);
        var addOns          = await _db.AddOns.CountAsync(cancellationToken);
        var ingredients     = await _db.Ingredients.CountAsync(cancellationToken);
        var inventoryItems  = await _db.InventoryItems.CountAsync(cancellationToken);
        var shifts          = await _db.Shifts.CountAsync(cancellationToken);
        var users           = await _db.Users.CountAsync(cancellationToken);
        var auditEvents     = await _db.AuditEvents.CountAsync(cancellationToken);

        var latestOrders        = await _db.Orders.MaxAsync(o => (DateTime?)o.UpdatedAt, cancellationToken);
        var latestCustomers     = await _db.Customers.MaxAsync(c => (DateTime?)c.UpdatedAt, cancellationToken);
        var latestMenuItems     = await _db.MenuItems.MaxAsync(m => (DateTime?)m.UpdatedAt, cancellationToken);
        var latestInventory     = await _db.InventoryItems.MaxAsync(i => (DateTime?)i.UpdatedAt, cancellationToken);

        return new AdminDatabaseStatsDto
        {
            Orders         = orders,
            Customers      = customers,
            MenuItems      = menuItems,
            AddOns         = addOns,
            Ingredients    = ingredients,
            InventoryItems = inventoryItems,
            Shifts         = shifts,
            Users          = users,
            AuditEvents    = auditEvents,
            LatestUpdates  = new AdminDatabaseLatestUpdatesDto
            {
                Orders         = latestOrders,
                Customers      = latestCustomers,
                MenuItems      = latestMenuItems,
                InventoryItems = latestInventory
            }
        };
    }
}
