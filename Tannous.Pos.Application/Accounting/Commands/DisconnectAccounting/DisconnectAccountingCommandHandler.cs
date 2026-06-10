using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Application.Accounting.Commands.DisconnectAccounting;

public class DisconnectAccountingCommandHandler : IRequestHandler<DisconnectAccountingCommand, bool>
{
    private readonly DbContext _dbContext;

    public DisconnectAccountingCommandHandler(DbContext dbContext) => _dbContext = dbContext;

    public async Task<bool> Handle(DisconnectAccountingCommand request, CancellationToken cancellationToken)
    {
        var connection = await _dbContext.Set<AccountingConnection>()
            .FirstOrDefaultAsync(c =>
                c.Provider == request.Provider
                && c.BranchId == request.BranchId
                && c.IsActive, cancellationToken);

        if (connection == null)
            throw new KeyNotFoundException($"No active {request.Provider} connection found.");

        connection.IsActive  = false;
        connection.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
