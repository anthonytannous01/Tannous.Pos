using Tannous.Pos.Domain.Common;

namespace Tannous.Pos.Domain.Entities;

public class Printer : BaseEntity, IAggregateRoot
{
    public string Name { get; set; } = string.Empty;
    public string PrinterType { get; set; } = string.Empty; // Receipt, Kitchen, Label, etc.
    public string? Model { get; set; }
    public string? IpAddress { get; set; }
    public int? Port { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Location { get; set; }
    
    // Foreign keys
    public Guid? DeviceId { get; set; }
    
    // Navigation properties
    public virtual Device? Device { get; set; }
}
