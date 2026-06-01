namespace Tannous.Pos.Application.Audit;

/// <summary>
/// Compact derived operational summary for forensic exports (counts and risk only).
/// GOVERNANCE: advisory only; full <see cref="OperationalForensicSnapshotDto"/> remains source-of-truth diagnostics.
/// NON-GOAL: no raw payloads, timeline bodies, metadata blobs, or replay receipt rows.
/// Cached upstream summaries used to build pressure fields may be stale within TTL.
/// </summary>
public sealed class OperationalForensicSnapshotSummaryDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public int ConflictCount { get; init; }
    public int AuditRecordCount { get; init; }
    public int ReplayReceiptCount { get; init; }
    public string CorrelatedIncidentRisk { get; init; } = OperationalIncidentSeverity.Low;
    public string OperationalPressureSummary { get; init; } = string.Empty;
    public string EscalationRisk { get; init; } = OperationalAlertSeverity.Info;
    public bool ContainsTruncatedData { get; init; }
    public string PrimarySubsystem { get; init; } = string.Empty;
}
