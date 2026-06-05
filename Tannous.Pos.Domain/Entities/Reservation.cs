using Tannous.Pos.Domain.Common;
using Tannous.Pos.Domain.Common.ValueObjects;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Domain.Entities;

public class Reservation : BaseEntity, IAggregateRoot
{
    public string  CustomerName  { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public int     PartySize     { get; set; } = 2;
    /// <summary>UTC datetime the reservation is booked for.</summary>
    public DateTime ReservationDateTime { get; set; }
    public string?  Notes        { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

    // Foreign keys
    /// <summary>Assigned table — may be null until customer arrives.</summary>
    public Guid?  TableId  { get; set; }
    public Guid?  BranchId { get; set; }

    // Navigation
    public virtual Table?  Table  { get; set; }
    public virtual Branch? Branch { get; set; }
}
