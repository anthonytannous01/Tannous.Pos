using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Infrastructure.Services;

public class SyncService : ISyncService
{
    private DateTime _lastSyncTime = DateTime.UtcNow;

    public async Task<bool> SyncDataAsync()
    {
        // Simulate data synchronization
        await Task.Delay(1000); // Simulate network delay
        _lastSyncTime = DateTime.UtcNow;
        return true;
    }

    public async Task<DateTime> GetLastSyncTimeAsync()
    {
        return await Task.FromResult(_lastSyncTime);
    }

    public async Task<bool> IsSyncRequiredAsync()
    {
        // Check if sync is required (e.g., based on time interval)
        var timeSinceLastSync = DateTime.UtcNow - _lastSyncTime;
        return await Task.FromResult(timeSinceLastSync.TotalMinutes > 30);
    }
}
