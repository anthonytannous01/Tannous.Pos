namespace Tannous.Pos.Application.Audit;

/// <summary>
/// Recommended operational data retention windows (governance guidance only — no automatic deletion).
/// </summary>
public static class OperationalRetentionConstants
{
    /// <summary>Primary diagnostics and live operator review (hot window).</summary>
    public const int HotOperationalWindowDays = 7;

    /// <summary>Reconciliation triage and replay mismatch review (warm window).</summary>
    public const int WarmReconciliationWindowDays = 30;

    /// <summary>Forensic snapshot / incident portability guidance (long-term window).</summary>
    public const int LongTermForensicWindowDays = 365;

    /// <summary>Maximum inclusive date span for internal audit/reconciliation queries.</summary>
    public const int MaxQueryDateRangeDays = 90;

    /// <summary>Unresolved conflict advisory threshold (operator review recommended).</summary>
    public const int UnresolvedAdvisoryDays = 7;

    /// <summary>Unresolved conflict elevated threshold (escalation review recommended).</summary>
    public const int UnresolvedElevatedDays = 30;

    /// <summary>Maximum timeline rows per internal query page expansion (aligns with forensic caps).</summary>
    public const int MaxTimelineExpansionItems = 500;

    /// <summary>Maximum conflicts aggregated per internal query expansion.</summary>
    public const int MaxConflictAggregationItems = 100;
}
