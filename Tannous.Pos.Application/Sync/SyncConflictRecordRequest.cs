namespace Tannous.Pos.Application.Sync;

/// <summary>Input for best-effort <see cref="ISyncConflictRecorder"/> persistence (no payload bodies).</summary>
public sealed class SyncConflictRecordRequest
{
    public string? DeviceId { get; init; }
    public string? OperationId { get; init; }
    public string? OperationType { get; init; }
    public string EntityType { get; init; } = string.Empty;
    public Guid? EntityId { get; init; }
    public string ConflictType { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public string? CorrelationId { get; init; }
    /// <summary>When true, skip insert if an unresolved record exists for DeviceId+OperationId+ConflictType.</summary>
    public bool DedupeByDeviceOperationAndType { get; init; }
}
