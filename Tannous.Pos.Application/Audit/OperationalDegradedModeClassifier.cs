namespace Tannous.Pos.Application.Audit;

/// <summary>Classifies primary degraded mode from safe aggregate metrics (no throttling).</summary>
public static class OperationalDegradedModeClassifier
{
    public static string ClassifyPrimary(OperationalResilienceMetricsSnapshot metrics)
    {
        if (metrics.RecentAuditPersistenceFailures >= OperationalResilienceConstants.RecentAuditPersistenceFailureThreshold)
            return OperationalDegradedModeTypes.AuditPersistencePressure;

        if (metrics.MaxReplayReceiptsOnSingleDevice >= OperationalResilienceConstants.ReplayStormDeviceReceiptThreshold
            || metrics.ReplayReceiptCount >= OperationalResilienceConstants.ReplayStormReceiptCountThreshold)
            return OperationalDegradedModeTypes.ReplayStormRisk;

        if (metrics.ForensicExportTruncated || metrics.TruncationWarningsIndicated)
            return OperationalDegradedModeTypes.ExportPressure;

        if (metrics.UnresolvedConflictCount >= OperationalResilienceConstants.HighUnresolvedConflictThreshold
            || metrics.UnresolvedOver7DaysCount >= OperationalResilienceConstants.ReconciliationBacklogElevatedThreshold)
            return OperationalDegradedModeTypes.ReconciliationPressure;

        if (metrics.QueryDateRangeClamped || metrics.QueryPageSizeClamped)
            return OperationalDegradedModeTypes.ElevatedQueryPressure;

        return OperationalDegradedModeTypes.Normal;
    }

    public static IReadOnlyList<string> ClassifyActiveModes(OperationalResilienceMetricsSnapshot metrics)
    {
        var modes = new List<string>();
        if (metrics.QueryDateRangeClamped || metrics.QueryPageSizeClamped)
            modes.Add(OperationalDegradedModeTypes.ElevatedQueryPressure);
        if (metrics.UnresolvedConflictCount >= OperationalResilienceConstants.ReconciliationBacklogElevatedThreshold)
            modes.Add(OperationalDegradedModeTypes.ReconciliationPressure);
        if (metrics.ForensicExportTruncated || metrics.TruncationWarningsIndicated)
            modes.Add(OperationalDegradedModeTypes.ExportPressure);
        if (metrics.RecentAuditPersistenceFailures > 0)
            modes.Add(OperationalDegradedModeTypes.AuditPersistencePressure);
        if (metrics.MaxReplayReceiptsOnSingleDevice >= OperationalResilienceConstants.ReplayStormDeviceReceiptThreshold
            || metrics.ReplayReceiptCount >= OperationalResilienceConstants.ReplayStormReceiptCountThreshold)
            modes.Add(OperationalDegradedModeTypes.ReplayStormRisk);

        if (modes.Count == 0)
            modes.Add(OperationalDegradedModeTypes.Normal);

        return modes;
    }

    public static string ClassifyReconciliationBacklogSeverity(OperationalResilienceMetricsSnapshot metrics)
    {
        if (metrics.UnresolvedConflictCount >= OperationalResilienceConstants.HighUnresolvedConflictThreshold)
            return "High";

        if (metrics.UnresolvedConflictCount >= OperationalResilienceConstants.ReconciliationBacklogElevatedThreshold)
            return "Elevated";

        return "Normal";
    }

    public static string ClassifyExportTruncationSeverity(ForensicTruncationFlags flags)
    {
        if (!flags.AnyTruncated)
            return "None";

        var count = 0;
        if (flags.AuditTimelineTruncated) count++;
        if (flags.ConflictRecordsTruncated) count++;
        if (flags.ReplayReceiptsTruncated) count++;
        if (flags.MetadataTruncated) count++;

        return count >= 3 ? "High" : count >= 2 ? "Elevated" : "Advisory";
    }

    public static string ClassifyExportPressure(OperationalResilienceMetricsSnapshot metrics, ForensicTruncationFlags? flags = null)
    {
        if (flags?.AnyTruncated == true)
            return ClassifyExportTruncationSeverity(flags);

        if (metrics.AuditRecordCount >= OperationalResilienceConstants.ForensicExportNearCapAuditRatio)
            return "Elevated";

        return "Normal";
    }
}
