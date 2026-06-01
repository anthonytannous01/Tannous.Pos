using Tannous.Pos.Domain.Common;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Domain.Entities;

/// <summary>Internal operational record for offline/sync reconciliation visibility (not exposed on mobile wire).</summary>
public class SyncConflictRecord : BaseEntity, IAggregateRoot
{
    public string? DeviceId { get; set; }
    public string? OperationId { get; set; }
    public string? OperationType { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string ConflictType { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public bool Resolved { get; set; }
    public string ResolutionStatus { get; set; } = ReconciliationResolutionStatus.Unresolved.ToString();
    public DateTime? ResolvedAtUtc { get; set; }
    public string? ResolutionNotes { get; set; }
    public string? ResolvedBy { get; set; }
}
