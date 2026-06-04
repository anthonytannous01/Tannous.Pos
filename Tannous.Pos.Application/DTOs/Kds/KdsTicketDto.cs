using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.DTOs.Kds;

/// <summary>
/// Represents a single order line as displayed on the Kitchen Display System.
/// </summary>
public class KdsTicketDto
{
    public Guid OrderLineId { get; set; }
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderType OrderType { get; set; }
    public string MenuItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Notes { get; set; }
    public List<string> AddOns { get; set; } = new();
    public KdsStatus KdsStatus { get; set; }
    public DateTime OrderCreatedAt { get; set; }
    public DateTime? KdsAcknowledgedAt { get; set; }
    public DateTime? KdsDoneAt { get; set; }

    /// <summary>Minutes elapsed since the order was placed. Used for colour-coding urgency.</summary>
    public int ElapsedMinutes =>
        (int)(DateTime.UtcNow - OrderCreatedAt).TotalMinutes;
}
