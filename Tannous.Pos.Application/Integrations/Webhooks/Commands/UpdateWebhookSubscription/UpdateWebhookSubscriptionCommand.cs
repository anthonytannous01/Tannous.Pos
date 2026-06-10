using MediatR;
using Tannous.Pos.Application.DTOs.Integrations;

namespace Tannous.Pos.Application.Integrations.Webhooks.Commands.UpdateWebhookSubscription;

public class UpdateWebhookSubscriptionCommand : IRequest<WebhookSubscriptionDto>
{
    public Guid Id { get; set; }
    public UpdateWebhookSubscriptionDto Subscription { get; set; } = new();
}
