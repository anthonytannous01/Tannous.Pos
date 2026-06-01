namespace Tannous.Pos.Application.Audit;

/// <summary>Safe aggregate inputs for degraded-mode and pressure classification.</summary>
public sealed class OperationalResilienceMetricsSnapshot
{
    public DateTime GeneratedAtUtc { get; init; }
    public int UnresolvedConflictCount { get; init; }
    public int UnresolvedOver7DaysCount { get; init; }
    public int ReplayReceiptCount { get; init; }
    public int MaxReplayReceiptsOnSingleDevice { get; init; }
    public int AuditRecordCount { get; init; }
    public int ReplayMismatchUnresolvedCount { get; init; }
    public int RecentAuditPersistenceFailures { get; init; }
    public bool TruncationWarningsIndicated { get; init; }
    public bool QueryDateRangeClamped { get; init; }
    public bool QueryPageSizeClamped { get; init; }
    public bool ForensicExportTruncated { get; init; }
}
