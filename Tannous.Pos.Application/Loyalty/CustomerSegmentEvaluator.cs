using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Loyalty;

/// <summary>
/// Lightweight, in-memory projection of a loyalty customer used for behavioural segmentation.
/// Kept deliberately small so analytics, segment listing, and campaign dispatch share one
/// evaluation path (no generic cognition engine — a single explicit primitive).
/// </summary>
public sealed class CustomerSegmentSnapshot
{
    public Guid CustomerId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public int LifetimePointsEarned { get; init; }
    public int PointBalance { get; init; }
    public int TotalOrders { get; init; }
    public DateTime? LastVisitDate { get; init; }
}

/// <summary>
/// Deterministic single-segment assignment and dataset-level VIP threshold computation.
/// Segment assignment uses fixed precedence so every customer maps to exactly one segment.
/// </summary>
public static class CustomerSegmentEvaluator
{
    public const int ActiveWindowDays = 30;
    public const int AtRiskWindowDays = 90;

    /// <summary>
    /// Computes the lifetime-points threshold for the VIP (top 20%) tier across the dataset.
    /// Returns null when there is no meaningful threshold (no accounts, or all zero points).
    /// </summary>
    public static int? ComputeVipThreshold(IReadOnlyCollection<CustomerSegmentSnapshot> snapshots)
    {
        if (snapshots == null || snapshots.Count == 0) return null;

        var ordered = snapshots
            .Select(s => s.LifetimePointsEarned)
            .OrderByDescending(p => p)
            .ToList();

        // Top 20% (at least one account when any exist).
        var topCount = (int)Math.Ceiling(ordered.Count * 0.2);
        if (topCount < 1) topCount = 1;

        var threshold = ordered[topCount - 1];
        return threshold > 0 ? threshold : (int?)null;
    }

    /// <summary>
    /// Assigns a single segment using fixed precedence:
    /// New (&lt;= 2 orders) → VipSpender (&gt;= VIP threshold) → recency tiers.
    /// </summary>
    public static CustomerSegment DetermineSegment(
        CustomerSegmentSnapshot snapshot,
        int? vipThreshold,
        DateTime utcNow)
    {
        if (snapshot.TotalOrders <= 2)
            return CustomerSegment.New;

        if (vipThreshold.HasValue && snapshot.LifetimePointsEarned >= vipThreshold.Value)
            return CustomerSegment.VipSpender;

        if (snapshot.LastVisitDate == null)
            return CustomerSegment.Lapsed;

        var daysSinceVisit = (utcNow - snapshot.LastVisitDate.Value).TotalDays;

        if (daysSinceVisit <= ActiveWindowDays)
            return CustomerSegment.ActiveRegular;

        if (daysSinceVisit <= AtRiskWindowDays)
            return CustomerSegment.AtRisk;

        return CustomerSegment.Lapsed;
    }
}
