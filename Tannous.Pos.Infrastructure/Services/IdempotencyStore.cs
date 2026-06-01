using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Infrastructure.Services;

public class IdempotencyStore : IIdempotencyStore
{
    private readonly PosDbContext _context;

    public IdempotencyStore(PosDbContext context)
    {
        _context = context;
    }

    public async Task<string?> GetResponseAsync(string key, string endpoint, CancellationToken cancellationToken = default)
    {
        var request = await _context.IdempotentRequests
            .FirstOrDefaultAsync(r => r.Key == key && r.Endpoint == endpoint && r.ExpiresAt > DateTime.UtcNow, cancellationToken);

        return request?.ResponseJson;
    }

    public async Task StoreResponseAsync(string key, string endpoint, string response, CancellationToken cancellationToken = default)
    {
        var existing = await _context.IdempotentRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Key == key && r.Endpoint == endpoint && r.ExpiresAt > DateTime.UtcNow, cancellationToken);
        if (existing != null)
        {
            return;
        }

        var responseHash = ComputeHash(response);
        
        var request = new IdempotentRequest
        {
            Key = key,
            Endpoint = endpoint,
            ResponseHash = responseHash,
            ResponseJson = response,
            ExpiresAt = DateTime.UtcNow.AddHours(24) // Expire after 24 hours
        };

        _context.IdempotentRequests.Add(request);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IsProcessedAsync(string key, string endpoint, CancellationToken cancellationToken = default)
    {
        return await _context.IdempotentRequests
            .AnyAsync(r => r.Key == key && r.Endpoint == endpoint && r.ExpiresAt > DateTime.UtcNow, cancellationToken);
    }

    private static string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
