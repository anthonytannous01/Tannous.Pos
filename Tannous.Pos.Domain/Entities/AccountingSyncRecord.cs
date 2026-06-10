using Tannous.Pos.Domain.Common;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Domain.Entities;

/// <summary>
/// One record per (Provider, BranchId, SyncDate) — prevents double-syncing a day.
/// </summary>
public class AccountingSyncRecord : BaseEntity, IAggregateRoot
{
    public AccountingProvider Provider { get; set; }
    public Guid?   BranchId           { get; set; }
    public DateTime SyncDate          { get; set; }
    public bool    IsSuccess          { get; set; }
    public string? ExternalReference  { get; set; }
    public string? ErrorMessage       { get; set; }
    public DateTime SyncedAt          { get; set; }

    public virtual Branch? Branch { get; set; }
}
