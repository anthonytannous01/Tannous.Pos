using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Application.DTOs.Loyalty;
using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Application.Loyalty.Queries.GetLoyaltyAccount;

public class GetLoyaltyAccountQueryHandler : IRequestHandler<GetLoyaltyAccountQuery, LoyaltyAccountDto?>
{
    private readonly DbContext _dbContext;

    public GetLoyaltyAccountQueryHandler(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<LoyaltyAccountDto?> Handle(GetLoyaltyAccountQuery request, CancellationToken cancellationToken)
    {
        var account = await _dbContext.Set<LoyaltyAccount>()
            .Include(la => la.Customer)
            .Include(la => la.Transactions.OrderByDescending(t => t.CreatedAt).Take(20))
            .FirstOrDefaultAsync(la => la.CustomerId == request.CustomerId && la.IsActive, cancellationToken);

        if (account == null) return null;

        return new LoyaltyAccountDto
        {
            Id                      = account.Id,
            CustomerId              = account.CustomerId,
            CustomerName            = $"{account.Customer.FirstName} {account.Customer.LastName}",
            PointBalance            = account.PointBalance,
            LifetimePointsEarned    = account.LifetimePointsEarned,
            LifetimePointsRedeemed  = account.LifetimePointsRedeemed,
            IsActive                = account.IsActive,
            CreatedAt               = account.CreatedAt,
            RecentTransactions      = account.Transactions.Select(t => new LoyaltyTransactionDto
            {
                Id              = t.Id,
                Points          = t.Points,
                TransactionType = t.TransactionType,
                OrderId         = t.OrderId,
                Notes           = t.Notes,
                CreatedAt       = t.CreatedAt
            }).ToList()
        };
    }
}
