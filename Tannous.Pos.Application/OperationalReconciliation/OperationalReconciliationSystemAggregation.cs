using Tannous.Pos.Application.OperationalBriefing;

namespace Tannous.Pos.Application.OperationalReconciliation;

/// <summary>
/// Static, deterministic aggregation for the reconciliation system view.
/// Synchronous. No snapshot stores. No async. Pure projection and classification.
/// </summary>
public static class OperationalReconciliationSystemAggregation
{
    public static OperationalReconciliationSystemDto ComposeReconciliationSystem(
        OperationalReconciliationAuditSummaryDto auditSummary,
        OperationalBriefingSummaryDto briefing,
        DateTime assessedAtUtc)
    {
        var health = ClassifySystemHealth(
            auditSummary.TotalUnresolvedConflicts,
            auditSummary.OldestUnresolvedConflictUtc,
            assessedAtUtc);

        return new OperationalReconciliationSystemDto
        {
            AssessedAtUtc                = assessedAtUtc,
            TotalUnresolvedConflicts     = auditSummary.TotalUnresolvedConflicts,
            OldestUnresolvedConflictUtc  = auditSummary.OldestUnresolvedConflictUtc,
            OrderScopedUnresolvedConflicts = auditSummary.OrderScopedUnresolvedConflicts,
            OtherEntityUnresolvedConflicts =
                auditSummary.TotalUnresolvedConflicts - auditSummary.OrderScopedUnresolvedConflicts,
            SystemHealth          = health,
            SystemHealthNarrative = ComposeNarrative(health, auditSummary.TotalUnresolvedConflicts,
                                        auditSummary.OldestUnresolvedConflictUtc, assessedAtUtc),
            SystemCognitionAge    = briefing.CognitionAge,
            SystemContextSummary  = briefing.BriefingSummary
        };
    }

    public static ReconciliationSystemHealth ClassifySystemHealth(
        int totalUnresolved,
        DateTime? oldestUtc,
        DateTime assessedAt)
    {
        if (totalUnresolved == 0) return ReconciliationSystemHealth.Stable;

        var oldestAgeHours = oldestUtc.HasValue
            ? (assessedAt - oldestUtc.Value).TotalHours
            : 0.0;

        if (totalUnresolved >= 20 || oldestAgeHours >= 24)
            return ReconciliationSystemHealth.Critical;

        if (totalUnresolved >= 10 || oldestAgeHours >= 4)
            return ReconciliationSystemHealth.Backlogged;

        if (totalUnresolved >= 3 || oldestAgeHours >= 1)
            return ReconciliationSystemHealth.Pressured;

        return ReconciliationSystemHealth.Stable;
    }

    private static string ComposeNarrative(
        ReconciliationSystemHealth health,
        int totalUnresolved,
        DateTime? oldestUtc,
        DateTime assessedAt)
    {
        return health switch
        {
            ReconciliationSystemHealth.Stable =>
                "Reconciliation system is stable. No unresolved conflicts detected.",

            ReconciliationSystemHealth.Pressured =>
                $"Reconciliation system under mild pressure. {totalUnresolved} unresolved conflict(s) detected.",

            ReconciliationSystemHealth.Backlogged =>
                $"Reconciliation backlog accumulating. {totalUnresolved} unresolved conflict(s); oldest pending {FormatAge(oldestUtc, assessedAt)}.",

            ReconciliationSystemHealth.Critical =>
                $"Reconciliation system in critical state. {totalUnresolved} unresolved conflict(s); oldest pending {FormatAge(oldestUtc, assessedAt)}.",

            _ => "Reconciliation system status undetermined."
        };
    }

    private static string FormatAge(DateTime? oldestUtc, DateTime assessedAt)
    {
        if (!oldestUtc.HasValue) return "unknown duration";
        var age = assessedAt - oldestUtc.Value;
        if (age.TotalHours >= 24) return $"{(int)age.TotalDays}d {age.Hours}h";
        if (age.TotalMinutes >= 60) return $"{(int)age.TotalHours}h {age.Minutes}m";
        return $"{(int)age.TotalMinutes}m";
    }
}
