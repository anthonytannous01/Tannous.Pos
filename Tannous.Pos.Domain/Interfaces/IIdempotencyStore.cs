namespace Tannous.Pos.Domain.Interfaces;

public interface IIdempotencyStore
{
    Task<string?> GetResponseAsync(string key, string endpoint, CancellationToken cancellationToken = default);
    Task StoreResponseAsync(string key, string endpoint, string response, CancellationToken cancellationToken = default);
    Task<bool> IsProcessedAsync(string key, string endpoint, CancellationToken cancellationToken = default);
}
