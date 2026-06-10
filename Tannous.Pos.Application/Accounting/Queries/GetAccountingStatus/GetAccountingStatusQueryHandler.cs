using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Application.DTOs.Accounting;
using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Application.Accounting.Queries.GetAccountingStatus;

public class GetAccountingStatusQueryHandler
    : IRequestHandler<GetAccountingStatusQuery, List<AccountingConnectionStatusDto>>
{
    private readonly DbContext _dbContext;

    public GetAccountingStatusQueryHandler(DbContext dbContext) => _dbContext = dbContext;

    public async Task<List<AccountingConnectionStatusDto>> Handle(
        GetAccountingStatusQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<AccountingConnection>()
            .AsNoTracking()
            .Where(c => c.IsActive);

        if (request.BranchId.HasValue)
            query = query.Where(c => c.BranchId == request.BranchId);

        var connections = await query.ToListAsync(cancellationToken);

        var result = new List<AccountingConnectionStatusDto>();
        foreach (var c in connections)
        {
            var syncCount = await _dbContext.Set<AccountingSyncRecord>()
                .CountAsync(r =>
                    r.Provider == c.Provider
                    && r.BranchId == c.BranchId
                    && r.IsSuccess, cancellationToken);

            result.Add(new AccountingConnectionStatusDto
            {
                Provider         = c.Provider.ToString(),
                IsConnected      = !string.IsNullOrWhiteSpace(c.AccessToken),
                CompanyName      = c.CompanyName,
                LastSyncAt       = c.LastSyncAt,
                LastSyncError    = c.LastSyncError,
                SyncRecordCount  = syncCount
            });
        }

        return result;
    }
}
