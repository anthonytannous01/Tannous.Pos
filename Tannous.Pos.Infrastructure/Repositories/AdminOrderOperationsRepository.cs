using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Domain.Interfaces;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Infrastructure.Repositories;

public class AdminOrderOperationsRepository : IAdminOrderOperationsRepository
{
    private readonly PosDbContext _db;

    public AdminOrderOperationsRepository(PosDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Order>> GetPaidOrdersWithoutReceiptsAsync(
        CancellationToken cancellationToken = default)
    {
        // No AsNoTracking — handler modifies ReceiptNumber on these entities before CommitAsync
        return await _db.Orders
            .Where(o => o.Status == OrderStatus.Paid && string.IsNullOrEmpty(o.ReceiptNumber))
            .OrderBy(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<string?> GetLastAssignedReceiptNumberAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.Orders
            .Where(o => !string.IsNullOrEmpty(o.ReceiptNumber))
            .OrderByDescending(o => o.ReceiptNumber)
            .Select(o => o.ReceiptNumber)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task CommitAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
