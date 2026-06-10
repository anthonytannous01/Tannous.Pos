using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Application.DTOs.Integrations;
using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Application.Integrations.ApiKeys.Queries.GetApiKeys;

public class GetApiKeysQueryHandler : IRequestHandler<GetApiKeysQuery, List<ApiKeyDto>>
{
    private readonly DbContext _dbContext;

    public GetApiKeysQueryHandler(DbContext dbContext) => _dbContext = dbContext;

    public async Task<List<ApiKeyDto>> Handle(GetApiKeysQuery request, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<ApiKey>()
            .AsNoTracking()
            .Where(k => k.IsActive)
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => new ApiKeyDto
            {
                Id         = k.Id,
                Name       = k.Name,
                KeyPrefix  = k.KeyPrefix,
                IsActive   = k.IsActive,
                ExpiresAt  = k.ExpiresAt,
                LastUsedAt = k.LastUsedAt,
                CreatedAt  = k.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }
}
