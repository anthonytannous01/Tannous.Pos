using Tannous.Pos.Domain.Common;

namespace Tannous.Pos.Domain.Entities;

public class AuditEvent : BaseEntity
{
    public DateTime Utc { get; set; }
    public Guid? UserId { get; set; }
    public string? DeviceId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? CorrelationId { get; set; }
    public string? PayloadJson { get; set; }

    // Navigation properties
    public User? User { get; set; }
}
