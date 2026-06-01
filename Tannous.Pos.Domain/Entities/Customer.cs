using Tannous.Pos.Domain.Common;

namespace Tannous.Pos.Domain.Entities;

public class Customer : BaseEntity, IAggregateRoot
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public string? Allergies { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastVisitDate { get; set; }
    public int TotalOrders { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public byte[] Version { get; set; } = new byte[8]; // Concurrency token for sync conflicts
    
    // Navigation properties
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
