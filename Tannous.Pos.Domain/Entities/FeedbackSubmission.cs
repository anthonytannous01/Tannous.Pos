using Tannous.Pos.Domain.Common;
using Tannous.Pos.Domain.Common.ValueObjects;

namespace Tannous.Pos.Domain.Entities;

/// <summary>
/// A customer feedback submission — collected post-order via the QR menu or POS receipt screen.
/// Public endpoint (no auth required to submit). Read-only for owners via authenticated API.
/// </summary>
public class FeedbackSubmission : BaseEntity, IAggregateRoot
{
    /// <summary>Overall rating 1–5.</summary>
    public int Rating { get; set; }

    /// <summary>Optional freetext comment.</summary>
    public string? Comment { get; set; }

    /// <summary>Feedback category selected by the customer.</summary>
    public FeedbackCategory Category { get; set; } = FeedbackCategory.General;

    /// <summary>Optional link to the order this feedback relates to.</summary>
    public Guid? OrderId { get; set; }

    /// <summary>Optional order number (denormalized for display without a join).</summary>
    public string? OrderNumber { get; set; }

    /// <summary>Optional customer name (anonymous if not provided).</summary>
    public string? CustomerName { get; set; }

    /// <summary>Branch the feedback relates to (if known).</summary>
    public Guid? BranchId { get; set; }

    // Navigation
    public virtual Order? Order { get; set; }
    public virtual Branch? Branch { get; set; }
}

public enum FeedbackCategory
{
    General   = 0,
    Food      = 1,
    Service   = 2,
    Delivery  = 3,
    Cleanliness = 4,
    Complaint = 5
}
