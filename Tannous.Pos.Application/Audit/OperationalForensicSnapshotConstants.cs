namespace Tannous.Pos.Application.Audit;

/// <summary>Caps for internal forensic snapshot aggregation (read-only export).</summary>
public static class OperationalForensicSnapshotConstants
{
    public const string SnapshotSchemaVersion = "1.0";
    public const int MaxAuditTimelineItems = 500;
    public const int MaxConflictRecords = 100;
    public const int MaxReplayReceipts = 50;
    public const int MaxSnapshotMetadataKeys = 40;
}
