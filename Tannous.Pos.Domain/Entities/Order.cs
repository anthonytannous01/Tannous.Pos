using System.ComponentModel.DataAnnotations;
using Tannous.Pos.Domain.Common;
using Tannous.Pos.Domain.Common.ValueObjects;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Domain.Entities;

public class Order : BaseEntity, IAggregateRoot
{
    /// <summary>EF optimistic concurrency token (PostgreSQL bytea). Not exposed on API DTOs.</summary>
    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;

    public string OrderNumber { get; set; } = string.Empty;
    public OrderType OrderType { get; set; }
    public OrderStatus Status { get; set; }
    public decimal SubTotal { get; set; } = 0;
    public decimal TaxAmount { get; set; } = 0;
    public decimal DiscountAmount { get; set; } = 0;
    /// <summary>
    /// USD stamp duty applied to this receipt per Lebanon's 2025 Budget Law.
    /// 0 when not applicable (StampDutyEnabled = false in BusinessSettings, or non-USD order).
    /// </summary>
    public decimal StampDutyAmount { get; set; } = 0;
    public decimal TotalAmount { get; set; } = 0;
    /// <summary>Total customer payments received at finalize (amount tendered).</summary>
    public decimal AmountTendered { get; set; } = 0;
    /// <summary>Change returned to customer: max(AmountTendered - TotalAmount, 0).</summary>
    public decimal ChangeDue { get; set; } = 0;
    /// <summary>Net sale amount retained: AmountTendered - ChangeDue (equals TotalAmount when fully settled).</summary>
    public decimal NetCapturedAmount { get; set; } = 0;
    public string? ReceiptNumber { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? Notes { get; set; }
    
    // Foreign keys
    public Guid? CustomerId { get; set; }
    public Guid? ShiftId { get; set; }
    public Guid? UserId { get; set; }
    /// <summary>Assigned restaurant table (DineIn orders only). Null for Takeaway/Delivery.</summary>
    public Guid? TableId { get; set; }
    /// <summary>Branch this order belongs to. Null only for legacy pre-branch data.</summary>
    public Guid? BranchId { get; set; }

    // Navigation properties
    public virtual Customer? Customer { get; set; }
    public virtual Shift? Shift { get; set; }
    public virtual User? User { get; set; }
    public virtual Table? Table { get; set; }
    public virtual Branch? Branch { get; set; }
    public virtual ICollection<OrderLine> OrderLines { get; set; } = new List<OrderLine>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public virtual ICollection<PaymentRefund> PaymentRefunds { get; set; } = new List<PaymentRefund>();
}
