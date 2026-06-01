using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Domain.Interfaces;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Infrastructure.Repositories;

public class CashDrawerEventRepository : ICashDrawerEventRepository
{
    private readonly PosDbContext _db;

    public CashDrawerEventRepository(PosDbContext db)
    {
        _db = db;
    }

    public Task<decimal> GetDropTotalAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        return _db.CashDrawerEvents
            .Where(cde => cde.EventType == "Drop"
                       && cde.Timestamp >= from
                       && cde.Timestamp < to)
            .SumAsync(cde => cde.Amount ?? 0m, cancellationToken);
    }
}
