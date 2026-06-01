using Tannous.Pos.Domain.Common;

namespace Tannous.Pos.Domain.Entities;

/// <summary>
/// Durable record of a completed sync push operation for replay-sensitive types (deviceId + operationId).
/// Used to short-circuit retries without double-applying money/inventory mutations.
/// </summary>
public class SyncOperationReceipt : BaseEntity
{
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>Client outbox operation id (mobile <c>operationId</c>).</summary>
    public string OperationId { get; set; } = string.Empty;

    public string OperationType { get; set; } = string.Empty;

    public bool Success { get; set; }

    public bool Conflict { get; set; }

    public string? ServerId { get; set; }

    /// <summary>Human-readable outcome message (success or error), replayed verbatim for client compatibility.</summary>
    public string? ResultMessage { get; set; }

    public DateTime ProcessedAtUtc { get; set; }
}
