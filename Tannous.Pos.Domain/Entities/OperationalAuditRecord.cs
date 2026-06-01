using Tannous.Pos.Domain.Common;

namespace Tannous.Pos.Domain.Entities;

/// <summary>
/// Append-only internal operational audit trail for money, inventory, replay, and lifecycle forensics.
/// Not exposed on mobile/API wire contracts.
/// </summary>
public class OperationalAuditRecord : BaseEntity, IAggregateRoot
{
    public string Category { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public Guid? OrderId { get; set; }
    public string? DeviceId { get; set; }
    public string? OperationId { get; set; }
    public string? CorrelationId { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
