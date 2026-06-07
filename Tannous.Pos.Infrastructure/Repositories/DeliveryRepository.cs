using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Domain.Interfaces;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Infrastructure.Repositories;

public class DeliveryRepository : IDeliveryRepository
{
    private readonly PosDbContext _db;

    public DeliveryRepository(PosDbContext db) => _db = db;

    public async Task<IEnumerable<DeliveryInfo>> GetQueueAsync(
        Guid? branchId, DeliveryStatus? status, DateTime? from, DateTime? to,
        CancellationToken ct = default)
    {
        var query = _db.DeliveryInfos
            .Include(d => d.Order)
            .AsNoTracking();

        if (branchId.HasValue)
            query = query.Where(d => d.BranchId == branchId.Value);

        if (status.HasValue)
            query = query.Where(d => d.Status == status.Value);
        else
            // Default: active deliveries only
            query = query.Where(d => d.Status != DeliveryStatus.Delivered
                                  && d.Status != DeliveryStatus.Failed
                                  && d.Status != DeliveryStatus.Cancelled);

        if (from.HasValue) query = query.Where(d => d.CreatedAt >= from.Value);
        if (to.HasValue)   query = query.Where(d => d.CreatedAt <= to.Value);

        return await query.OrderBy(d => d.CreatedAt).ToListAsync(ct);
    }

    public Task<DeliveryInfo?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default)
        => _db.DeliveryInfos
            .Include(d => d.Order)
            .FirstOrDefaultAsync(d => d.OrderId == orderId, ct);

    public Task<DeliveryInfo?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.DeliveryInfos
            .Include(d => d.Order)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task AddAsync(DeliveryInfo delivery, CancellationToken ct = default)
        => await _db.DeliveryInfos.AddAsync(delivery, ct);

    public Task CommitAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
