using Tannous.Pos.Domain.Common;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Domain.Entities;

/// <summary>
/// A one-shot loyalty marketing campaign dispatched to a behavioural segment via WhatsApp.
/// Append-only record of intent + dispatch outcome. Never coupled to the Order core.
/// </summary>
public class LoyaltyCampaign : BaseEntity, IAggregateRoot
{
    public string Name { get; set; } = string.Empty;

    /// <summary>WhatsApp message body (operator-authored).</summary>
    public string Message { get; set; } = string.Empty;

    public CustomerSegment TargetSegment { get; set; }

    /// <summary>Number of customers resolved into the target segment at send time.</summary>
    public int RecipientCount { get; set; }

    /// <summary>Number of messages successfully accepted by the notification provider.</summary>
    public int SentCount { get; set; }

    public CampaignStatus Status { get; set; } = CampaignStatus.Pending;

    public Guid CreatedByUserId { get; set; }

    public DateTime? SentAt { get; set; }

    public string? ErrorMessage { get; set; }
}
