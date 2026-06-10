using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Infrastructure.Services.Accounting;

/// <summary>
/// Runs daily sales sync across all active accounting connections.
/// </summary>
public sealed class AccountingSyncCoordinator : IAccountingSyncCoordinator
{
    private readonly DbContext _dbContext;
    private readonly IEnumerable<IAccountingSync> _syncServices;
    private readonly ILogger<AccountingSyncCoordinator> _logger;

    public AccountingSyncCoordinator(
        DbContext dbContext,
        IEnumerable<IAccountingSync> syncServices,
        ILogger<AccountingSyncCoordinator> logger)
    {
        _dbContext     = dbContext;
        _syncServices  = syncServices;
        _logger        = logger;
    }

    public async Task<(int Synced, List<string> Errors)> RunSyncAsync(
        DateTime date, Guid? branchId, CancellationToken ct = default)
    {
        var syncDate = date.Date;
        var synced   = 0;
        var errors   = new List<string>();

        var connectionsQuery = _dbContext.Set<AccountingConnection>()
            .Where(c => c.IsActive);

        if (branchId.HasValue)
            connectionsQuery = connectionsQuery.Where(c => c.BranchId == branchId);

        var connections = await connectionsQuery.ToListAsync(ct);

        foreach (var connection in connections)
        {
            var alreadySynced = await _dbContext.Set<AccountingSyncRecord>()
                .AnyAsync(r =>
                    r.Provider == connection.Provider
                    && r.BranchId == connection.BranchId
                    && r.SyncDate == syncDate
                    && r.IsSuccess, ct);

            if (alreadySynced) continue;

            var syncService = _syncServices.FirstOrDefault(s => s.Provider == connection.Provider);
            if (syncService == null) continue;

            var (success, externalRef, error) = await syncService.SyncDayAsync(connection, syncDate, ct);

            _dbContext.Set<AccountingSyncRecord>().Add(new AccountingSyncRecord
            {
                Provider          = connection.Provider,
                BranchId          = connection.BranchId,
                SyncDate          = syncDate,
                IsSuccess         = success,
                ExternalReference = externalRef,
                ErrorMessage      = error,
                SyncedAt          = DateTime.UtcNow
            });

            connection.LastSyncAt    = DateTime.UtcNow;
            connection.LastSyncError = success ? null : error;

            await _dbContext.SaveChangesAsync(ct);

            if (success)
                synced++;
            else if (!string.IsNullOrWhiteSpace(error))
                errors.Add($"{connection.Provider}: {error}");

            _logger.LogInformation(
                "Accounting sync for {Provider}, date {Date}: {Result}",
                connection.Provider, syncDate.ToString("yyyy-MM-dd"),
                success ? "OK" : $"FAILED — {error}");
        }

        return (synced, errors);
    }
}
