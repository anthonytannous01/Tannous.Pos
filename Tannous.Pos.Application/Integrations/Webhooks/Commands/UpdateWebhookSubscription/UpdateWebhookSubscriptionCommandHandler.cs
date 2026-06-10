using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Application.DTOs.Integrations;
using Tannous.Pos.Application.Integrations.Webhooks.Queries.GetWebhookSubscriptions;
using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Application.Integrations.Webhooks.Commands.UpdateWebhookSubscription;

public class UpdateWebhookSubscriptionCommandHandler
    : IRequestHandler<UpdateWebhookSubscriptionCommand, WebhookSubscriptionDto>
{
    private readonly DbContext _dbContext;

    public UpdateWebhookSubscriptionCommandHandler(DbContext dbContext) => _dbContext = dbContext;

    public async Task<WebhookSubscriptionDto> Handle(
        UpdateWebhookSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await _dbContext.Set<WebhookSubscription>()
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Webhook subscription {request.Id} not found.");

        var dto = request.Subscription;
        var events = GetWebhookSubscriptionsQueryHandler.ParseEvents(dto.Events).ToList();
        if (events.Count == 0)
            throw new InvalidOperationException("At least one event type is required.");

        subscription.Name        = dto.Name.Trim();
        subscription.EndpointUrl = dto.EndpointUrl.Trim();
        subscription.IsActive    = dto.IsActive;
        subscription.SetSubscribedEvents(events);
        subscription.UpdatedAt   = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var lastLog = await _dbContext.Set<WebhookDeliveryLog>()
            .AsNoTracking()
            .Where(l => l.SubscriptionId == subscription.Id)
            .OrderByDescending(l => l.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return GetWebhookSubscriptionsQueryHandler.MapToDto(subscription, lastLog);
    }
}
