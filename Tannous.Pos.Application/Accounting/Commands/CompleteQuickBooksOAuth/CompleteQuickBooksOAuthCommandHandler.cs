using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Accounting.Commands.CompleteQuickBooksOAuth;

public class CompleteQuickBooksOAuthCommandHandler : IRequestHandler<CompleteQuickBooksOAuthCommand, bool>
{
    private readonly IEnumerable<IAccountingSync> _syncServices;
    private readonly DbContext                _dbContext;

    public CompleteQuickBooksOAuthCommandHandler(
        IEnumerable<IAccountingSync> syncServices,
        DbContext dbContext)
    {
        _syncServices = syncServices;
        _dbContext    = dbContext;
    }

    public async Task<bool> Handle(CompleteQuickBooksOAuthCommand request, CancellationToken cancellationToken)
    {
        var quickBooks = _syncServices.FirstOrDefault(s => s.Provider == AccountingProvider.QuickBooks)
            ?? throw new InvalidOperationException("QuickBooks sync service is not registered.");

        var success = await quickBooks.ExchangeCodeAsync(request.Code, request.State, cancellationToken);
        if (!success) return false;

        if (!string.IsNullOrWhiteSpace(request.RealmId))
        {
            Guid? branchGuid = null;
            if (!string.IsNullOrWhiteSpace(request.State) && Guid.TryParse(request.State, out var parsed))
                branchGuid = parsed;

            var connection = await _dbContext.Set<AccountingConnection>()
                .FirstOrDefaultAsync(c =>
                    c.Provider == AccountingProvider.QuickBooks && c.BranchId == branchGuid,
                    cancellationToken);

            if (connection != null)
            {
                connection.CompanyId = request.RealmId;
                connection.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        return true;
    }
}
