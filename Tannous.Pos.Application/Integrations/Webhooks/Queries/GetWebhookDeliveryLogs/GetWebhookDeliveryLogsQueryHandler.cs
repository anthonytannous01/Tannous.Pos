using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Application.DTOs.Integrations;
using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Application.Integrations.Webhooks.Queries.GetWebhookDeliveryLogs;

public class GetWebhookDeliveryLogsQueryHandler
    : IRequestHandler<GetWebhookDeliveryLogsQuery, List<WebhookDeliveryLogDto>>
{
    private readonly DbContext _dbContext;

    public GetWebhookDeliveryLogsQueryHandler(DbContext dbContext) => _dbContext = dbContext;

    public async Task<List<WebhookDeliveryLogDto>> Handle(
        GetWebhookDeliveryLogsQuery request, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Set<WebhookSubscription>()
            .AnyAsync(s => s.Id == request.SubscriptionId, cancellationToken);
        if (!exists)
            throw new KeyNotFoundException($"Webhook subscription {request.SubscriptionId} not found.");

        return await _dbContext.Set<WebhookDeliveryLog>()
            .AsNoTracking()
            .Where(l => l.SubscriptionId == request.SubscriptionId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(50)
            .Select(l => new WebhookDeliveryLogDto
            {
                Id           = l.Id,
                EventId      = l.EventId,
                EventType    = l.EventType.ToString(),
                ResponseCode = l.ResponseCode,
                IsSuccess    = l.IsSuccess,
                ErrorMessage = l.ErrorMessage,
                DurationMs   = l.DurationMs,
                CreatedAt    = l.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }
}
