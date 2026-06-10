namespace Tannous.Pos.Domain.Interfaces;

/// <summary>
/// Orchestrates daily sales sync across all active accounting connections.
/// </summary>
public interface IAccountingSyncCoordinator
{
    Task<(int Synced, List<string> Errors)> RunSyncAsync(
        DateTime          date,
        Guid?             branchId,
        CancellationToken ct = default);
}
