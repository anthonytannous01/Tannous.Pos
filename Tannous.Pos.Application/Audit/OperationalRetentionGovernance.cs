namespace Tannous.Pos.Application.Audit;

/// <summary>
/// Operational data lifecycle governance (retention assumptions, query expectations, non-goals).
/// GOVERNANCE: Append-only operational history — no automatic pruning, no physical archival provider,
/// no S3/Azure/immutable compliance vault, no background deletion workers.
/// </summary>
public static class OperationalRetentionGovernance
{
    public static string ClassifyRetention(DateTime createdAtUtc, DateTime utcNow)
    {
        var age = utcNow - createdAtUtc;
        if (age.TotalDays <= OperationalRetentionConstants.HotOperationalWindowDays)
            return OperationalRetentionCategories.HotOperational;

        if (age.TotalDays <= OperationalRetentionConstants.WarmReconciliationWindowDays)
            return OperationalRetentionCategories.WarmReconciliation;

        return OperationalRetentionCategories.LongTermForensic;
    }

    public static string GetQueryExpectation(string retentionCategory) =>
        retentionCategory switch
        {
            OperationalRetentionCategories.HotOperational =>
                "Prefer narrow filters; default pagination; ideal for live incident triage.",
            OperationalRetentionCategories.WarmReconciliation =>
                "Use bounded date ranges; review unresolved conflicts and replay mismatches.",
            OperationalRetentionCategories.LongTermForensic =>
                "Use forensic export snapshots; expect query clamping and truncation warnings.",
            _ => "Use bounded date ranges and internal Admin diagnostics only."
        };

    public static string GetOperationalWarning(string retentionCategory) =>
        retentionCategory switch
        {
            OperationalRetentionCategories.HotOperational => string.Empty,
            OperationalRetentionCategories.WarmReconciliation =>
                "Data is outside the hot operational window; reconciliation review recommended.",
            OperationalRetentionCategories.LongTermForensic =>
                "Data is aged; rely on forensic export portability and avoid unbounded scans.",
            _ => string.Empty
        };
}
