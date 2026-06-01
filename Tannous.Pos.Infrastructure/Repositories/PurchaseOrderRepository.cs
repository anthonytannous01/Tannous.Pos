using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Infrastructure.Repositories;

public class PurchaseOrderRepository : IPurchaseOrderRepository
{
    private readonly PosDbContext _context;

    public PurchaseOrderRepository(PosDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PurchaseOrder>> GetAllAsync()
    {
        return await _context.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.Lines)
            .ThenInclude(line => line.Ingredient)
            .ToListAsync();
    }

    public async Task<PurchaseOrder?> GetByIdAsync(Guid id)
    {
        return await _context.PurchaseOrders.FindAsync(id);
    }

    public async Task<PurchaseOrder?> GetByIdWithDetailsAsync(Guid id)
    {
        return await _context.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.Lines)
            .ThenInclude(line => line.Ingredient)
            .FirstOrDefaultAsync(po => po.Id == id);
    }

    public async Task<IEnumerable<PurchaseOrder>> GetBySupplierAsync(Guid supplierId)
    {
        return await _context.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.Lines)
            .ThenInclude(line => line.Ingredient)
            .Where(po => po.SupplierId == supplierId)
            .ToListAsync();
    }

    public async Task<IEnumerable<PurchaseOrder>> GetByStatusAsync(string status)
    {
        return await _context.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.Lines)
            .ThenInclude(line => line.Ingredient)
            .Where(po => po.Status == status)
            .ToListAsync();
    }

    public async Task AddAsync(PurchaseOrder purchaseOrder)
    {
        await _context.PurchaseOrders.AddAsync(purchaseOrder);
    }

    public async Task UpdateAsync(PurchaseOrder purchaseOrder)
    {
        _context.PurchaseOrders.Update(purchaseOrder);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var purchaseOrder = await _context.PurchaseOrders.FindAsync(id);
        if (purchaseOrder != null)
        {
            _context.PurchaseOrders.Remove(purchaseOrder);
        }
    }
}
