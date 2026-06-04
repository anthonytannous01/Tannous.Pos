using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.DTOs.Loyalty;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Loyalty.Commands.EarnPoints;

public class EarnPointsCommandHandler : IRequestHandler<EarnPointsCommand, LoyaltyAccountDto>
{
    private readonly DbContext _dbContext;
    private readonly ILogger<EarnPointsCommandHandler> _logger;

    public EarnPointsCommandHandler(DbContext dbContext, ILogger<EarnPointsCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<LoyaltyAccountDto> Handle(EarnPointsCommand request, CancellationToken cancellationToken)
    {
        if (request.Points <= 0)
            throw new InvalidOperationException("Points to earn must be positive");

        // Get or create loyalty account
        var account = await _dbContext.Set<LoyaltyAccount>()
            .Include(la => la.Customer)
            .FirstOrDefaultAsync(la => la.CustomerId == request.CustomerId && la.IsActive, cancellationToken);

        if (account == null)
        {
            var customer = await _dbContext.Set<Customer>()
                .FindAsync(new object[] { request.CustomerId }, cancellationToken);
            if (customer == null)
                throw new InvalidOperationException($"Customer {request.CustomerId} not found");

            account = new LoyaltyAccount
            {
                CustomerId = request.CustomerId,
                Customer   = customer
            };
            _dbContext.Set<LoyaltyAccount>().Add(account);
        }

        account.PointBalance         += request.Points;
        account.LifetimePointsEarned += request.Points;
        account.UpdatedAt             = DateTime.UtcNow;

        var transaction = new LoyaltyTransaction
        {
            LoyaltyAccountId = account.Id,
            Points           = request.Points,
            TransactionType  = LoyaltyTransactionType.Earn,
            OrderId          = request.OrderId,
            Notes            = request.Notes ?? $"Earned on order"
        };
        _dbContext.Set<LoyaltyTransaction>().Add(transaction);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Loyalty points earned. CustomerId={CustomerId}, Points={Points}, NewBalance={Balance}, OrderId={OrderId}",
            request.CustomerId, request.Points, account.PointBalance, request.OrderId);

        return MapToDto(account);
    }

    private static LoyaltyAccountDto MapToDto(LoyaltyAccount account) => new()
    {
        Id                     = account.Id,
        CustomerId             = account.CustomerId,
        CustomerName           = $"{account.Customer.FirstName} {account.Customer.LastName}",
        PointBalance           = account.PointBalance,
        LifetimePointsEarned   = account.LifetimePointsEarned,
        LifetimePointsRedeemed = account.LifetimePointsRedeemed,
        IsActive               = account.IsActive,
        CreatedAt              = account.CreatedAt
    };
}
