using Tannous.Pos.Domain.Common;
using Tannous.Pos.Domain.Common.ValueObjects;

namespace Tannous.Pos.Domain.Entities;

public class CashDrawerEvent : BaseEntity, IAggregateRoot
{
    public string EventType { get; set; } = string.Empty; // Open, Close, Drop, Sale, Refund, etc.
    public decimal? Amount { get; set; }
    /// <summary>Physical currency of Amount ("USD" or "LBP"). Drawer math reconciles each currency separately.</summary>
    public string Currency { get; set; } = "USD";
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public DateTime EventDate { get; set; }
    public DateTime Timestamp { get; set; }
    
    // Foreign keys
    public Guid ShiftId { get; set; }
    
    // Navigation properties
    public virtual Shift Shift { get; set; } = null!;
}
