using Tannous.Pos.Application.Audit;

namespace Tannous.Pos.Application.OperationalEntityStatus;

/// <summary>
/// Deterministic entity health classification and narrative composition.
/// No record arrays. No ordinal enum comparison. Explicit threshold conditions only.
/// </summary>
public static class OperationalEntityStatusAggregation
{
    public static EntityHealthClassification ClassifyHealth(
        int auditCount,
        string highestSeverity,
        int unresolvedConflictCount)
    {
        if (auditCount == 0)
            return EntityHealthClassification.Unknown;

        if (highestSeverity == OperationalAuditSeverity.Critical || unresolvedConflictCount >= 3)
            return EntityHealthClassification.Critical;

        var hasWarning = highestSeverity == OperationalAuditSeverity.Warning;
        var hasConflicts = unresolvedConflictCount > 0;

        if (hasWarning && hasConflicts)
            return EntityHealthClassification.AtRisk;

        if (unresolvedConflictCount >= 2)
            return EntityHealthClassification.AtRisk;

        if (hasWarning || unresolvedConflictCount == 1)
            return EntityHealthClassification.Watchable;

        return EntityHealthClassification.Healthy;
    }

    public static string ComposeOrderNarrative(
        int auditCount,
        string highestSeverity,
        int unresolvedConflictCount,
        EntityHealthClassification classification)
    {
        if (auditCount == 0)
            return "No operational audit records found for this order";

        return classification switch
        {
            EntityHealthClassification.Critical =>
                $"Order has critical signals: severity={highestSeverity}, unresolved conflicts={unresolvedConflictCount}",
            EntityHealthClassification.AtRisk =>
                $"Order requires attention: {unresolvedConflictCount} unresolved conflict(s), severity={highestSeverity}",
            EntityHealthClassification.Watchable =>
                $"Order within bounds: {auditCount} audit record(s), severity={highestSeverity}, conflicts={unresolvedConflictCount}",
            EntityHealthClassification.Healthy =>
                $"Order operationally clear: {auditCount} audit record(s), no unresolved conflicts",
            _ => "Order status undetermined"
        };
    }

    public static string ComposeDeviceNarrative(
        int auditCount,
        string highestSeverity,
        int unresolvedConflictCount,
        int receiptTotal,
        int receiptConflictCount,
        EntityHealthClassification classification)
    {
        if (auditCount == 0 && receiptTotal == 0)
            return "No operational records found for this device";

        return classification switch
        {
            EntityHealthClassification.Critical =>
                $"Device has critical signals: severity={highestSeverity}, unresolved conflicts={unresolvedConflictCount}, receipt conflicts={receiptConflictCount}/{receiptTotal}",
            EntityHealthClassification.AtRisk =>
                $"Device requires attention: {unresolvedConflictCount} unresolved conflict(s), severity={highestSeverity}",
            EntityHealthClassification.Watchable =>
                $"Device within bounds: {auditCount} audit record(s), {receiptTotal} receipt(s), severity={highestSeverity}",
            EntityHealthClassification.Healthy =>
                $"Device operationally clear: {auditCount} audit record(s), {receiptTotal} receipt(s), no unresolved conflicts",
            _ => "Device status undetermined"
        };
    }
}
