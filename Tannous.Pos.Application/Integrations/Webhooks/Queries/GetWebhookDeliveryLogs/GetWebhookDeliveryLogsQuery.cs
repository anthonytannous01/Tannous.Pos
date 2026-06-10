using MediatR;
using Tannous.Pos.Application.DTOs.Integrations;

namespace Tannous.Pos.Application.Integrations.Webhooks.Queries.GetWebhookDeliveryLogs;

public class GetWebhookDeliveryLogsQuery : IRequest<List<WebhookDeliveryLogDto>>
{
    public Guid SubscriptionId { get; set; }
}
