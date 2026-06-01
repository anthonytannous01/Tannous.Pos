namespace Tannous.Pos.Application.Sync;

/// <summary>Safe projection of a sync conflict record for internal reconciliation diagnostics.</summary>
public sealed class SyncConflictItemDto
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
    public string AgingSeverity { get; init; } = string.Empty;
    public string EscalationRecommendation { get; init; } = string.Empty;
}
