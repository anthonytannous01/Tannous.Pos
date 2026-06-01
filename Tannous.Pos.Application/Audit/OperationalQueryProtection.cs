namespace Tannous.Pos.Application.Audit;

/// <summary>Normalized/clamped query window for internal operational diagnostics (no deletion).</summary>
public sealed class OperationalQueryRangeResult
{
    public DateTime? EffectiveFromUtc { get; init; }
    public DateTime? EffectiveToUtc { get; init; }
    public bool DateRangeClamped { get; init; }
    public int RequestedRangeDays { get; init; }
    public int AppliedRangeDays { get; init; }
}

/// <summary>Date-range and expansion protections for internal audit/reconciliation/export queries.</summary>
public static class OperationalQueryProtection
{
    public static OperationalQueryRangeResult NormalizeDateRange(
        DateTime? fromUtc,
        DateTime? toUtc,
        DateTime utcNow)
    {
        if (!fromUtc.HasValue && !toUtc.HasValue)
        {
            return new OperationalQueryRangeResult
            {
                EffectiveFromUtc = null,
                EffectiveToUtc = null,
                DateRangeClamped = false,
                RequestedRangeDays = 0,
                AppliedRangeDays = 0
            };
        }

        var effectiveTo = toUtc ?? utcNow;
        if (effectiveTo > utcNow)
            effectiveTo = utcNow;

        var effectiveFrom = fromUtc ?? effectiveTo.AddDays(-OperationalRetentionConstants.MaxQueryDateRangeDays);

        if (effectiveFrom > effectiveTo)
            (effectiveFrom, effectiveTo) = (effectiveTo, effectiveFrom);

        var requestedDays = Math.Max(1, (int)Math.Ceiling((effectiveTo - effectiveFrom).TotalDays));
        var clamped = false;

        if (requestedDays > OperationalRetentionConstants.MaxQueryDateRangeDays)
        {
            effectiveFrom = effectiveTo.AddDays(-OperationalRetentionConstants.MaxQueryDateRangeDays);
            clamped = true;
        }

        var appliedDays = Math.Max(1, (int)Math.Ceiling((effectiveTo - effectiveFrom).TotalDays));

        return new OperationalQueryRangeResult
        {
            EffectiveFromUtc = effectiveFrom,
            EffectiveToUtc = effectiveTo,
            DateRangeClamped = clamped,
            RequestedRangeDays = requestedDays,
            AppliedRangeDays = appliedDays
        };
    }

    public static int NormalizePageSize(int pageSize) =>
        pageSize <= 0
            ? OperationalAuditQueryConstants.DefaultPageSize
            : Math.Min(pageSize, OperationalAuditQueryConstants.MaxPageSize);

    public static int NormalizePage(int page) =>
        page >= OperationalAuditQueryConstants.MinPage
            ? page
            : OperationalAuditQueryConstants.MinPage;
}
