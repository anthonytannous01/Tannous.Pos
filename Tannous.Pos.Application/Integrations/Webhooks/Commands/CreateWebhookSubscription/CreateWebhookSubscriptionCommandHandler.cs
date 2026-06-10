using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Tannous.Pos.Application.DTOs.Integrations;
using Tannous.Pos.Application.Integrations.Webhooks.Queries.GetWebhookSubscriptions;
using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Application.Integrations.Webhooks.Commands.CreateWebhookSubscription;

public class CreateWebhookSubscriptionCommandHandler
    : IRequestHandler<CreateWebhookSubscriptionCommand, CreateWebhookResponse>
{
    private readonly DbContext _dbContext;

    public CreateWebhookSubscriptionCommandHandler(DbContext dbContext) => _dbContext = dbContext;

    public async Task<CreateWebhookResponse> Handle(
        CreateWebhookSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Subscription;
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("Webhook name is required.");
        if (string.IsNullOrWhiteSpace(dto.EndpointUrl))
            throw new InvalidOperationException("Endpoint URL is required.");

        var events = GetWebhookSubscriptionsQueryHandler.ParseEvents(dto.Events).ToList();
        if (events.Count == 0)
            throw new InvalidOperationException("At least one event type is required.");

        var secret = "whsec_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();

        var subscription = new WebhookSubscription
        {
            Name        = dto.Name.Trim(),
            EndpointUrl = dto.EndpointUrl.Trim(),
            Secret      = secret,
            BranchId    = dto.BranchId,
            IsActive    = true
        };
        subscription.SetSubscribedEvents(events);

        _dbContext.Set<WebhookSubscription>().Add(subscription);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var mapped = GetWebhookSubscriptionsQueryHandler.MapToDto(subscription, lastLog: null);
        return new CreateWebhookResponse
        {
            Id                    = mapped.Id,
            Name                  = mapped.Name,
            EndpointUrl           = mapped.EndpointUrl,
            IsActive              = mapped.IsActive,
            BranchId              = mapped.BranchId,
            Events                = mapped.Events,
            CreatedAt             = mapped.CreatedAt,
            LastDeliveryAt        = mapped.LastDeliveryAt,
            LastDeliverySucceeded = mapped.LastDeliverySucceeded,
            Secret                = secret
        };
    }
}
