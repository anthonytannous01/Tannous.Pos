namespace Tannous.Pos.Domain.Enums;

/// <summary>
/// Behavioural segmentation of loyalty customers for CRM analytics and campaigns.
/// A customer is assigned exactly one segment via deterministic precedence
/// (see CustomerSegmentEvaluator). Analytics counts may overlap by design.
/// </summary>
public enum CustomerSegment
{
    /// <summary>LifetimePointsEarned in the top 20% of accounts.</summary>
    VipSpender    = 0,

    /// <summary>LastVisitDate within 30 days and TotalOrders &gt;= 3.</summary>
    ActiveRegular = 1,

    /// <summary>LastVisitDate between 31 and 90 days ago.</summary>
    AtRisk        = 2,

    /// <summary>LastVisitDate &gt; 90 days ago, or null with prior orders.</summary>
    Lapsed        = 3,

    /// <summary>TotalOrders &lt;= 2 (newly acquired customer).</summary>
    New           = 4
}
