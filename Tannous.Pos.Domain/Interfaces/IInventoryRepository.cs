using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Domain.Interfaces;

public interface IInventoryRepository : IRepository<InventoryItem>
{
    Task<IEnumerable<InventoryItem>> GetLowStockItemsAsync();
    Task<InventoryItem?> GetByIngredientAsync(Guid ingredientId);
    Task<IEnumerable<InventoryItem>> GetByStockLevelAsync(decimal minStock, decimal maxStock);
    Task AddMovementAsync(InventoryMovement movement);
    Task<IEnumerable<InventoryItem>> GetAllWithIngredientAsync();
    Task<InventoryItem?> GetByIdWithIngredientAsync(Guid id);
    Task AddWastageAsync(WastageRecord wastage);
    Task CommitAsync(CancellationToken cancellationToken = default);
}
