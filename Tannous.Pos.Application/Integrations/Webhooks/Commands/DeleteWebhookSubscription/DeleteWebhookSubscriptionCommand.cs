using MediatR;

namespace Tannous.Pos.Application.Integrations.Webhooks.Commands.DeleteWebhookSubscription;

public class DeleteWebhookSubscriptionCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}
