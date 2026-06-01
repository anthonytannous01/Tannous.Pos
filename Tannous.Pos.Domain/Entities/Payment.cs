using Tannous.Pos.Domain.Common;
using Tannous.Pos.Domain.Common.ValueObjects;

namespace Tannous.Pos.Domain.Entities;

public class Payment : BaseEntity, IAggregateRoot
{
    public string PaymentMethod { get; set; } = string.Empty; // Cash, Card, Mobile, etc.
    public decimal Amount { get; set; } = 0;
    public string? TransactionId { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public DateTime PaymentDate { get; set; }
    public bool IsSuccessful { get; set; } = true;
    
    // Foreign keys
    public Guid OrderId { get; set; }
    
    // Navigation properties
    public virtual Order Order { get; set; } = null!;
}
