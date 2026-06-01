using Tannous.Pos.Application.DTOs.Sync;

namespace Tannous.Pos.Application.Sync;

/// <summary>
/// Wraps replay-sensitive sync processors with durable short-circuit (same deviceId + operationId) under a serializable transaction.
/// </summary>
public interface IDurableSyncReplayCoordinator
{
    Task<OpResultDto> ExecuteAsync(
        string? deviceId,
        string opId,
        string operationType,
        Func<Task<OpResultDto>> operation,
        CancellationToken cancellationToken = default);
}
