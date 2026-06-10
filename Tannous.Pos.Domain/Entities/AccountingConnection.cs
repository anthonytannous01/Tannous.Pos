using Tannous.Pos.Domain.Common;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Domain.Entities;

/// <summary>
/// Stores the OAuth2 tokens for one accounting provider.
/// One row per provider per branch (max two rows in a single-branch setup).
/// Tokens stored as plain text — production deployments should encrypt at rest via Postgres TDE or a vault.
/// </summary>
public class AccountingConnection : BaseEntity, IAggregateRoot
{
    public AccountingProvider Provider    { get; set; }
    public Guid?   BranchId              { get; set; }
    public bool    IsActive              { get; set; } = true;
    public string  AccessToken           { get; set; } = string.Empty;
    public string  RefreshToken          { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }
    /// <summary>QBO: realm ID (company). Xero: tenant ID.</summary>
    public string  CompanyId             { get; set; } = string.Empty;
    public string? CompanyName           { get; set; }
    public DateTime? LastSyncAt          { get; set; }
    public string? LastSyncError         { get; set; }

    public virtual Branch? Branch { get; set; }
}
