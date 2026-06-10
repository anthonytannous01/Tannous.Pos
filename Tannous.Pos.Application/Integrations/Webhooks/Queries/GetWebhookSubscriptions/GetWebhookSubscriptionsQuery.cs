using MediatR;
using Tannous.Pos.Application.DTOs.Integrations;

namespace Tannous.Pos.Application.Integrations.Webhooks.Queries.GetWebhookSubscriptions;

public class GetWebhookSubscriptionsQuery : IRequest<List<WebhookSubscriptionDto>>
{
}
