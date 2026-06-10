using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using Tannous.Pos.Application.DTOs.Integrations;
using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Application.Integrations.ApiKeys.Commands.CreateApiKey;

public class CreateApiKeyCommandHandler : IRequestHandler<CreateApiKeyCommand, CreateApiKeyResponse>
{
    private readonly DbContext _dbContext;

    public CreateApiKeyCommandHandler(DbContext dbContext) => _dbContext = dbContext;

    public async Task<CreateApiKeyResponse> Handle(
        CreateApiKeyCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ApiKey.Name))
            throw new InvalidOperationException("API key name is required.");

        var raw = "tnp_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
        var prefix = raw[..8];

        var entity = new ApiKey
        {
            Name      = request.ApiKey.Name.Trim(),
            KeyHash   = hash,
            KeyPrefix = prefix,
            BranchId  = request.ApiKey.BranchId,
            ExpiresAt = request.ApiKey.ExpiresAt,
            IsActive  = true
        };

        _dbContext.Set<ApiKey>().Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CreateApiKeyResponse
        {
            Id         = entity.Id,
            Name       = entity.Name,
            KeyPrefix  = entity.KeyPrefix,
            IsActive   = entity.IsActive,
            ExpiresAt  = entity.ExpiresAt,
            LastUsedAt = entity.LastUsedAt,
            CreatedAt  = entity.CreatedAt,
            RawKey     = raw
        };
    }
}
