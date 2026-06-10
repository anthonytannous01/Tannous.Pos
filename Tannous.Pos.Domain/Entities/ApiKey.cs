using Tannous.Pos.Domain.Common;

namespace Tannous.Pos.Domain.Entities;

/// <summary>
/// Long-lived read-only API key for third-party integrators.
/// Grants access to read-only endpoints (reports, menu, customers) without a staff JWT.
/// Never grants write access.
/// </summary>
public class ApiKey : BaseEntity, IAggregateRoot
{
    public string  Name       { get; set; } = string.Empty;
    public string  KeyHash    { get; set; } = string.Empty;
    public string  KeyPrefix  { get; set; } = string.Empty;
    public bool    IsActive   { get; set; } = true;
    public Guid?   BranchId   { get; set; }
    public DateTime? ExpiresAt  { get; set; }
    public DateTime? LastUsedAt { get; set; }

    public virtual Branch? Branch { get; set; }
}
