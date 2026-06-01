using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Audit;

/// <summary>
/// Stale unresolved conflict detection and escalation recommendations (query/DTO enrichment only).
/// NO background jobs, NO automatic escalation, NO notifications.
/// </summary>
public static class OperationalConflictLifecycleClassifier
{
    private static readonly string[] OpenStatuses =
    {
        nameof(ReconciliationResolutionStatus.Unresolved),
        nameof(ReconciliationResolutionStatus.Acknowledged),
        nameof(ReconciliationResolutionStatus.Investigating)
    };

    public static bool IsUnresolved(string resolutionStatus) =>
        OpenStatuses.Contains(resolutionStatus, StringComparer.Ordinal);

    public static string ClassifyAgingSeverity(DateTime createdAtUtc, string resolutionStatus, DateTime utcNow)
    {
        if (!IsUnresolved(resolutionStatus))
            return OperationalConflictAgingSeverity.None;

        var ageDays = (utcNow - createdAtUtc).TotalDays;
        if (ageDays >= OperationalRetentionConstants.UnresolvedElevatedDays)
            return OperationalConflictAgingSeverity.Critical;

        if (ageDays >= OperationalRetentionConstants.UnresolvedAdvisoryDays)
            return OperationalConflictAgingSeverity.Elevated;

        return OperationalConflictAgingSeverity.Advisory;
    }

    public static string GetEscalationRecommendation(
        string conflictType,
        string agingSeverity,
        int replayMismatchCountOnDevice = 0,
        int inventoryDriftCountOnDevice = 0)
    {
        if (agingSeverity == OperationalConflictAgingSeverity.None)
            return string.Empty;

        if (agingSeverity == OperationalConflictAgingSeverity.Critical)
            return "Manual operator review required: unresolved conflict exceeded 30 days.";

        if (replayMismatchCountOnDevice >= 3
            || conflictType.Contains("ReplayMismatch", StringComparison.OrdinalIgnoreCase))
            return "Review replay mismatch accumulation; verify device operationId reuse.";

        if (inventoryDriftCountOnDevice >= 2
            || conflictType.Contains("InventoryDrift", StringComparison.OrdinalIgnoreCase))
            return "Review repeated inventory drift signals; reconcile stock movements.";

        if (agingSeverity == OperationalConflictAgingSeverity.Elevated)
            return "Unresolved over 7 days; acknowledge or investigate via reconciliation workflow.";

        return "Monitor unresolved conflict; use forensic export if incident persists.";
    }
}
