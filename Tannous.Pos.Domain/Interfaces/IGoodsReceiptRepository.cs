using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Domain.Interfaces;

public interface IGoodsReceiptRepository
{
    Task<IEnumerable<GoodsReceipt>> GetAllAsync();
    Task<GoodsReceipt?> GetByIdAsync(Guid id);
    Task<GoodsReceipt?> GetByIdWithDetailsAsync(Guid id);
    Task<IEnumerable<GoodsReceipt>> GetByPurchaseOrderAsync(Guid purchaseOrderId);
    Task AddAsync(GoodsReceipt goodsReceipt);
    Task UpdateAsync(GoodsReceipt goodsReceipt);
    Task DeleteAsync(Guid id);
}
