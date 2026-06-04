using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.DTOs.Loyalty;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Loyalty.Commands.RedeemPoints;

public class RedeemPointsCommandHandler : IRequestHandler<RedeemPointsCommand, LoyaltyAccountDto>
{
    private readonly DbContext _dbContext;
    private readonly IBusinessSettingsRepository _settingsRepository;
    private readonly ILogger<RedeemPointsCommandHandler> _logger;

    public RedeemPointsCommandHandler(
        DbContext dbContext,
        IBusinessSettingsRepository settingsRepository,
        ILogger<RedeemPointsCommandHandler> logger)
    {
        _dbContext         = dbContext;
        _settingsRepository = settingsRepository;
        _logger            = logger;
    }

    public async Task<LoyaltyAccountDto> Handle(RedeemPointsCommand request, CancellationToken cancellationToken)
    {
        if (request.Points <= 0)
            throw new InvalidOperationException("Points to redeem must be positive");

        var settings = await _settingsRepository.GetAsync(cancellationToken);
        var minRedeem = settings?.LoyaltyMinRedeemPoints ?? 100;

        var account = await _dbContext.Set<LoyaltyAccount>()
            .Include(la => la.Customer)
            .FirstOrDefaultAsync(la => la.CustomerId == request.CustomerId && la.IsActive, cancellationToken);

        if (account == null)
            throw new InvalidOperationException($"No active loyalty account for customer {request.CustomerId}");

        if (account.PointBalance < minRedeem)
            throw new InvalidOperationException(
                $"Minimum {minRedeem} points required to redeem. Current balance: {account.PointBalance}");

        if (request.Points > account.PointBalance)
            throw new InvalidOperationException(
                $"Cannot redeem {request.Points} points — balance is only {account.PointBalance}");

        account.PointBalance           -= request.Points;
        account.LifetimePointsRedeemed += request.Points;
        account.UpdatedAt               = DateTime.UtcNow;

        var transaction = new LoyaltyTransaction
        {
            LoyaltyAccountId = account.Id,
            Points           = -request.Points,
            TransactionType  = LoyaltyTransactionType.Redeem,
            OrderId          = request.OrderId,
            Notes            = "Points redeemed at checkout"
        };
        _dbContext.Set<LoyaltyTransaction>().Add(transaction);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Loyalty points redeemed. CustomerId={CustomerId}, Points={Points}, NewBalance={Balance}",
            request.CustomerId, request.Points, account.PointBalance);

        return new LoyaltyAccountDto
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
}
