namespace Tannous.Pos.Application.Audit;

/// <summary>Operational resilience thresholds (visibility/governance only — no automatic throttling).</summary>
public static class OperationalResilienceConstants
{
    public const int HighUnresolvedConflictThreshold = 25;
    public const int ReconciliationBacklogElevatedThreshold = 10;
    public const int ReplayStormReceiptCountThreshold = 100;
    public const int ReplayStormDeviceReceiptThreshold = 40;
    public const int LargeAuditVolumeThreshold = 1000;
    public const int RecentAuditPersistenceFailureThreshold = 3;
    public const int ForensicExportNearCapAuditRatio = 450;
    public const int ForensicExportNearCapConflictRatio = 80;
}
