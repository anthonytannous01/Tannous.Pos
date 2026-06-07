using Tannous.Pos.Domain.Common;
using Tannous.Pos.Domain.Common.ValueObjects;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Domain.Entities;

/// <summary>
/// Delivery-specific information for an order with OrderType = Delivery.
/// Kept as a separate aggregate so delivery concerns never pollute the core Order model.
/// One DeliveryInfo per Order — enforced by unique index on OrderId.
/// </summary>
public class DeliveryInfo : BaseEntity, IAggregateRoot
{
    // ── Order link ────────────────────────────────────────────────────────────
    public Guid OrderId { get; set; }

    // ── Delivery details ──────────────────────────────────────────────────────
    public string         DeliveryAddress   { get; set; } = string.Empty;
    public string?        ApartmentDetails  { get; set; }  // floor, building, etc.
    public string?        CustomerPhone     { get; set; }
    public DeliveryChannel Channel          { get; set; } = DeliveryChannel.Own;
    public DeliveryStatus Status            { get; set; } = DeliveryStatus.Pending;
    public decimal        DeliveryFee       { get; set; } = 0m;
    /// <summary>Estimated delivery time in minutes.</summary>
    public int?           EstimatedMinutes  { get; set; }
    public string?        Notes             { get; set; }

    // ── Driver ────────────────────────────────────────────────────────────────
    public string?        DriverName        { get; set; }
    public string?        DriverPhone       { get; set; }

    // ── Timestamps ───────────────────────────────────────────────────────────
    public DateTime?      AssignedAt        { get; set; }
    public DateTime?      PickedUpAt        { get; set; }
    public DateTime?      DeliveredAt       { get; set; }

    // ── Branch ───────────────────────────────────────────────────────────────
    public Guid?          BranchId          { get; set; }

    // ── Navigation ───────────────────────────────────────────────────────────
    public virtual Order?  Order            { get; set; }
    public virtual Branch? Branch           { get; set; }
}
