using Tannous.Pos.Domain.Common;
using Tannous.Pos.Domain.Common.ValueObjects;

namespace Tannous.Pos.Domain.Entities;

public class CashDrawerEvent : BaseEntity, IAggregateRoot
{
    public string EventType { get; set; } = string.Empty; // Open, Close, Drop, Sale, Refund, etc.
    public decimal? Amount { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public DateTime EventDate { get; set; }
    public DateTime Timestamp { get; set; }
    
    // Foreign keys
    public Guid ShiftId { get; set; }
    
    // Navigation properties
    public virtual Shift Shift { get; set; } = null!;
}
