using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.DTOs.Loyalty;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Loyalty.Commands.EarnPoints;

public class EarnPointsCommandHandler : IRequestHandler<EarnPointsCommand, LoyaltyAccountDto>
{
    private readonly DbContext _dbContext;
    private readonly INotificationService _notificationService;
    private readonly IWebhookDispatcher _webhookDispatcher;
    private readonly ILogger<EarnPointsCommandHandler> _logger;

    public EarnPointsCommandHandler(
        DbContext dbContext,
        INotificationService notificationService,
        IWebhookDispatcher webhookDispatcher,
        ILogger<EarnPointsCommandHandler> logger)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
        _webhookDispatcher = webhookDispatcher;
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

        var settings = await _dbContext.Set<BusinessSettings>()
            .FirstOrDefaultAsync(cancellationToken);

        if (settings?.NotifyOnLoyaltyEarn == true &&
            !string.IsNullOrWhiteSpace(account.Customer?.Phone))
        {
            _logger.LogDebug(
                "Sending points-earned notification. CustomerId={CustomerId}, Points={Points}",
                request.CustomerId, request.Points);

            _ = _notificationService.SendPointsEarnedNotificationAsync(
                toPhone:           account.Customer.Phone,
                pointsEarned:      request.Points,
                newBalance:        account.PointBalance,
                businessName:      settings.BusinessName,
                cancellationToken: cancellationToken);
        }

        _ = _webhookDispatcher.DispatchAsync(
            WebhookEventType.LoyaltyPointsEarned,
            new
            {
                customerId   = request.CustomerId,
                pointsEarned = request.Points,
                newBalance   = account.PointBalance,
                orderId      = request.OrderId
            },
            cancellationToken: cancellationToken);

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
