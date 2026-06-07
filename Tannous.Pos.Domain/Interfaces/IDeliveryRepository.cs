using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Domain.Interfaces;

public interface IDeliveryRepository
{
    /// <summary>Get delivery orders — active queue by default (Pending/Assigned/PickedUp/OnWay).</summary>
    Task<IEnumerable<DeliveryInfo>> GetQueueAsync(
        Guid? branchId, DeliveryStatus? status, DateTime? from, DateTime? to,
        CancellationToken ct = default);

    Task<DeliveryInfo?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);
    Task<DeliveryInfo?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(DeliveryInfo delivery, CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
}
