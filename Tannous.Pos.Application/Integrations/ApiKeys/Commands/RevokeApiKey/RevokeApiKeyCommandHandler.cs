using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Application.Integrations.ApiKeys.Commands.RevokeApiKey;

public class RevokeApiKeyCommandHandler : IRequestHandler<RevokeApiKeyCommand, bool>
{
    private readonly DbContext _dbContext;

    public RevokeApiKeyCommandHandler(DbContext dbContext) => _dbContext = dbContext;

    public async Task<bool> Handle(RevokeApiKeyCommand request, CancellationToken cancellationToken)
    {
        var key = await _dbContext.Set<ApiKey>()
            .FirstOrDefaultAsync(k => k.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"API key {request.Id} not found.");

        key.IsActive  = false;
        key.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
