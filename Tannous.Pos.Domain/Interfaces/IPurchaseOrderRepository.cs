using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Domain.Interfaces;

public interface IPurchaseOrderRepository
{
    Task<IEnumerable<PurchaseOrder>> GetAllAsync();
    Task<PurchaseOrder?> GetByIdAsync(Guid id);
    Task<PurchaseOrder?> GetByIdWithDetailsAsync(Guid id);
    Task<IEnumerable<PurchaseOrder>> GetBySupplierAsync(Guid supplierId);
    Task<IEnumerable<PurchaseOrder>> GetByStatusAsync(string status);
    Task AddAsync(PurchaseOrder purchaseOrder);
    Task UpdateAsync(PurchaseOrder purchaseOrder);
    Task DeleteAsync(Guid id);
}
