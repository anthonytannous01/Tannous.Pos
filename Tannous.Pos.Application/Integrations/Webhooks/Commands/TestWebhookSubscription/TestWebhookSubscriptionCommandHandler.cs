using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Integrations.Webhooks.Commands.TestWebhookSubscription;

public class TestWebhookSubscriptionCommandHandler : IRequestHandler<TestWebhookSubscriptionCommand, bool>
{
    private readonly DbContext _dbContext;
    private readonly IWebhookDispatcher _dispatcher;

    public TestWebhookSubscriptionCommandHandler(DbContext dbContext, IWebhookDispatcher dispatcher)
    {
        _dbContext  = dbContext;
        _dispatcher = dispatcher;
    }

    public async Task<bool> Handle(TestWebhookSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await _dbContext.Set<WebhookSubscription>()
            .FirstOrDefaultAsync(s => s.Id == request.Id && s.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException($"Webhook subscription {request.Id} not found.");

        await _dispatcher.DispatchAsync(
            WebhookEventType.OrderFinalized,
            new
            {
                orderId     = Guid.Empty,
                orderNumber = "TEST-001",
                total       = 0m,
                currency    = "USD",
                customerId  = (Guid?)null,
                orderType   = "Test",
                test        = true
            },
            branchId: subscription.BranchId,
            subscriptionId: subscription.Id,
            cancellationToken: cancellationToken);

        return true;
    }
}
