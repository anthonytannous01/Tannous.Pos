namespace Tannous.Pos.Application.Audit;

/// <summary>Safe projection of a sync conflict for forensic export (no payloads/stacks).</summary>
public sealed class ConflictSnapshotItemDto
{
    public Guid Id { get; init; }
    public string? DeviceId { get; init; }
    public string? OperationId { get; init; }
    public string? OperationType { get; init; }
    public string EntityType { get; init; } = string.Empty;
    public Guid? EntityId { get; init; }
    public string ConflictType { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public string? CorrelationId { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public string ResolutionStatus { get; init; } = string.Empty;
    public string? ResolutionNotes { get; init; }
    public string? ResolvedBy { get; init; }
    public DateTime? ResolvedAtUtc { get; init; }
}
