using MediatR;
using System.Security.Cryptography;
using Tannous.Pos.Application.DTOs.Integrations;

namespace Tannous.Pos.Application.Integrations.Webhooks.Commands.CreateWebhookSubscription;

public class CreateWebhookSubscriptionCommand : IRequest<CreateWebhookResponse>
{
    public CreateWebhookSubscriptionDto Subscription { get; set; } = new();
}
