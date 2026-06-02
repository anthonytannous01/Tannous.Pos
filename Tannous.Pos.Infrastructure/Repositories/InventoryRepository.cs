using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Infrastructure.Repositories;

public class InventoryRepository : Repository<InventoryItem>, IInventoryRepository
{
    public InventoryRepository(PosDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<InventoryItem>> GetLowStockItemsAsync()
    {
        return await _dbSet
            .Include(ii => ii.Ingredient)
            .Where(ii => ii.Ingredient.IsActive && ii.CurrentStock <= ii.MinimumStock)
            .ToListAsync();
    }

    public async Task<InventoryItem?> GetByIngredientAsync(Guid ingredientId)
    {
        return await _dbSet
            .Include(ii => ii.Ingredient)
            .FirstOrDefaultAsync(ii => ii.IngredientId == ingredientId);
    }

    public async Task<IEnumerable<InventoryItem>> GetByStockLevelAsync(decimal minStock, decimal maxStock)
    {
        return await _dbSet
            .Include(ii => ii.Ingredient)
            .Where(ii => ii.CurrentStock >= minStock && ii.CurrentStock <= maxStock)
            .ToListAsync();
    }

    public async Task AddMovementAsync(InventoryMovement movement)
    {
        await _context.InventoryMovements.AddAsync(movement);
    }

    public async Task<IEnumerable<InventoryItem>> GetAllWithIngredientAsync()
    {
        // Filter to active ingredients only — inactive ingredients should not
        // appear in the stock screen after they have been deactivated.
        return await _dbSet
            .Include(ii => ii.Ingredient)
            .Where(ii => ii.Ingredient.IsActive)
            .ToListAsync();
    }

    public async Task<InventoryItem?> GetByIdWithIngredientAsync(Guid id)
    {
        return await _dbSet
            .Include(ii => ii.Ingredient)
            .FirstOrDefaultAsync(ii => ii.Id == id);
    }

    public async Task AddWastageAsync(WastageRecord wastage)
    {
        await _context.WastageRecords.AddAsync(wastage);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
