using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Application.DTOs.Integrations;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Integrations.Webhooks.Queries.GetWebhookSubscriptions;

public class GetWebhookSubscriptionsQueryHandler
    : IRequestHandler<GetWebhookSubscriptionsQuery, List<WebhookSubscriptionDto>>
{
    private readonly DbContext _dbContext;

    public GetWebhookSubscriptionsQueryHandler(DbContext dbContext) => _dbContext = dbContext;

    public async Task<List<WebhookSubscriptionDto>> Handle(
        GetWebhookSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        var subs = await _dbContext.Set<WebhookSubscription>()
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

        var result = new List<WebhookSubscriptionDto>();
        foreach (var s in subs)
        {
            var lastLog = await _dbContext.Set<WebhookDeliveryLog>()
                .AsNoTracking()
                .Where(l => l.SubscriptionId == s.Id)
                .OrderByDescending(l => l.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            result.Add(MapToDto(s, lastLog));
        }

        return result;
    }

    internal static WebhookSubscriptionDto MapToDto(WebhookSubscription s, WebhookDeliveryLog? lastLog) => new()
    {
        Id                    = s.Id,
        Name                  = s.Name,
        EndpointUrl           = s.EndpointUrl,
        IsActive              = s.IsActive,
        BranchId              = s.BranchId,
        Events                = s.GetSubscribedEvents().Select(e => e.ToString()).ToList(),
        CreatedAt             = s.CreatedAt,
        LastDeliveryAt        = lastLog?.CreatedAt,
        LastDeliverySucceeded = lastLog?.IsSuccess
    };

    internal static IEnumerable<WebhookEventType> ParseEvents(IEnumerable<string> eventNames)
    {
        foreach (var name in eventNames)
        {
            if (Enum.TryParse<WebhookEventType>(name, ignoreCase: true, out var parsed))
                yield return parsed;
        }
    }
}
