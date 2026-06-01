using Tannous.Pos.Domain.Common;

namespace Tannous.Pos.Domain.Entities;

public class Device : BaseEntity, IAggregateRoot
{
    public string DeviceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty; // POS Terminal, Kitchen Display, etc.
    public string? SerialNumber { get; set; }
    public string? Location { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastSyncDate { get; set; }
    public string? IpAddress { get; set; }
    public string? MacAddress { get; set; }
    
    // Navigation properties
    public virtual ICollection<Printer> Printers { get; set; } = new List<Printer>();
}
