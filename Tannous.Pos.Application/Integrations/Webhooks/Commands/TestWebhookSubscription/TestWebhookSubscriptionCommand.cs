using MediatR;

namespace Tannous.Pos.Application.Integrations.Webhooks.Commands.TestWebhookSubscription;

public class TestWebhookSubscriptionCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}
