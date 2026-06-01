namespace Tannous.Pos.Application.Audit;

/// <summary>Detects operational pressure patterns for visibility (no request rejection beyond existing clamps).</summary>
public static class OperationalPressureClassifier
{
    public static IReadOnlyDictionary<string, bool> BuildPressureIndicators(OperationalResilienceMetricsSnapshot metrics) =>
        new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            ["oversizedForensicExportRisk"] = metrics.ForensicExportTruncated || metrics.TruncationWarningsIndicated,
            ["largeRangeDiagnosticsQuery"] = metrics.QueryDateRangeClamped,
            ["excessivePaginationRequest"] = metrics.QueryPageSizeClamped,
            ["highVolumeUnresolvedConflicts"] = metrics.UnresolvedConflictCount
                >= OperationalResilienceConstants.ReconciliationBacklogElevatedThreshold,
            ["largeReplayReceiptAggregation"] = metrics.ReplayReceiptCount
                >= OperationalResilienceConstants.ReplayStormReceiptCountThreshold,
            ["replayStormPerDeviceRisk"] = metrics.MaxReplayReceiptsOnSingleDevice
                >= OperationalResilienceConstants.ReplayStormDeviceReceiptThreshold,
            ["auditPersistenceDegraded"] = metrics.RecentAuditPersistenceFailures > 0,
            ["largeAuditVolume"] = metrics.AuditRecordCount >= OperationalResilienceConstants.LargeAuditVolumeThreshold
        };
}
