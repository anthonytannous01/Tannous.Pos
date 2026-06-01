using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Infrastructure.Repositories;

public class GoodsReceiptRepository : IGoodsReceiptRepository
{
    private readonly PosDbContext _context;

    public GoodsReceiptRepository(PosDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<GoodsReceipt>> GetAllAsync()
    {
        return await _context.GoodsReceipts
            .Include(gr => gr.PurchaseOrder)
            .Include(gr => gr.Lines)
            .ThenInclude(line => line.Ingredient)
            .ToListAsync();
    }

    public async Task<GoodsReceipt?> GetByIdAsync(Guid id)
    {
        return await _context.GoodsReceipts.FindAsync(id);
    }

    public async Task<GoodsReceipt?> GetByIdWithDetailsAsync(Guid id)
    {
        return await _context.GoodsReceipts
            .Include(gr => gr.PurchaseOrder)
            .Include(gr => gr.Lines)
            .ThenInclude(line => line.Ingredient)
            .FirstOrDefaultAsync(gr => gr.Id == id);
    }

    public async Task<IEnumerable<GoodsReceipt>> GetByPurchaseOrderAsync(Guid purchaseOrderId)
    {
        return await _context.GoodsReceipts
            .Include(gr => gr.PurchaseOrder)
            .Include(gr => gr.Lines)
            .ThenInclude(line => line.Ingredient)
            .Where(gr => gr.PurchaseOrderId == purchaseOrderId)
            .ToListAsync();
    }

    public async Task AddAsync(GoodsReceipt goodsReceipt)
    {
        await _context.GoodsReceipts.AddAsync(goodsReceipt);
    }

    public async Task UpdateAsync(GoodsReceipt goodsReceipt)
    {
        _context.GoodsReceipts.Update(goodsReceipt);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var goodsReceipt = await _context.GoodsReceipts.FindAsync(id);
        if (goodsReceipt != null)
        {
            _context.GoodsReceipts.Remove(goodsReceipt);
        }
    }
}
