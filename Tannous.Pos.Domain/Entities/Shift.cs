using System.ComponentModel.DataAnnotations;
using Tannous.Pos.Domain.Common;
using Tannous.Pos.Domain.Common.ValueObjects;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Domain.Entities;

public class Shift : BaseEntity, IAggregateRoot
{
    /// <summary>EF optimistic concurrency token (PostgreSQL bytea).</summary>
    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;

    public string ShiftNumber { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public decimal OpeningBalance { get; set; } = 0;
    public decimal? ClosingBalance { get; set; }
    public decimal? ExpectedCash { get; set; }
    public decimal? ActualCash { get; set; }
    public decimal? CashDifference { get; set; }

    // Dual-currency drawer (Lebanon): the same physical drawer holds USD and LBP notes.
    // Each currency is opened, tracked, and reconciled independently — never converted.
    public decimal OpeningBalanceLbp { get; set; } = 0;
    public decimal? ExpectedCashLbp { get; set; }
    public decimal? ActualCashLbp { get; set; }
    public decimal? CashDifferenceLbp { get; set; }
    public ShiftStatus Status { get; set; }
    public string? Notes { get; set; }
    
    // Foreign keys
    public Guid UserId { get; set; }
    /// <summary>Branch this shift belongs to. Null only for legacy pre-branch data.</summary>
    public Guid? BranchId { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Branch? Branch { get; set; }
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    public virtual ICollection<CashDrawerEvent> CashDrawerEvents { get; set; } = new List<CashDrawerEvent>();
}
