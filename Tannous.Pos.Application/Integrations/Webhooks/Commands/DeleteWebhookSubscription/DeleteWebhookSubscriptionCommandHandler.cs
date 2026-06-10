using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Application.Integrations.Webhooks.Commands.DeleteWebhookSubscription;

public class DeleteWebhookSubscriptionCommandHandler : IRequestHandler<DeleteWebhookSubscriptionCommand, bool>
{
    private readonly DbContext _dbContext;

    public DeleteWebhookSubscriptionCommandHandler(DbContext dbContext) => _dbContext = dbContext;

    public async Task<bool> Handle(DeleteWebhookSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await _dbContext.Set<WebhookSubscription>()
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Webhook subscription {request.Id} not found.");

        subscription.IsActive  = false;
        subscription.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
