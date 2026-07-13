using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Domain.Interfaces;

public interface IOrderRepository : IRepository<Order>
{
    Task<Order?> GetByIdWithDetailsAsync(Guid id);
    Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status);
    Task<IEnumerable<Order>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<Order?> GetByOrderNumberAsync(string orderNumber);
    Task<IEnumerable<Order>> GetByCustomerAsync(Guid customerId);
    Task<IEnumerable<Order>> GetByShiftAsync(Guid shiftId);
    Task<IEnumerable<Order>> GetPaidOrdersInDateRangeAsync(DateTime from, DateTime to);

    /// <summary>True if any order line references the menu item (order-history guard for deletes).</summary>
    Task<bool> AnyOrderLineForMenuItemAsync(Guid menuItemId);

    /// <summary>True if any order line add-on references the add-on (order-history guard for deletes).</summary>
    Task<bool> AnyOrderLineForAddOnAsync(Guid addOnId);
}
