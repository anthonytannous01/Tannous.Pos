namespace Tannous.Pos.Application.Audit;

/// <summary>
/// Compact internal forensic snapshot for incident investigation (read-only, safe projection).
/// GOVERNANCE: Operational artifact for portability — not legal evidence, not an immutable archive.
/// </summary>
public sealed class OperationalForensicSnapshotDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public DateTime SnapshotGeneratedUtc { get; init; }
    public string SnapshotSchemaVersion { get; init; } = OperationalForensicSnapshotConstants.SnapshotSchemaVersion;
    public string ExportSource { get; init; } = string.Empty;
    public string RetentionClassification { get; init; } = string.Empty;
    public ForensicTruncationFlags TruncationFlags { get; init; } = new();
    public string ExportPressureClassification { get; init; } = "Normal";
    public string TruncationSeverity { get; init; } = "None";
    public string? ExportSurvivabilityWarning { get; init; }
    public string CorrelatedIncidentRisk { get; init; } = OperationalIncidentSeverity.Low;
    public IReadOnlyList<string> CorrelatedSubsystems { get; init; } = Array.Empty<string>();
    public string IncidentCorrelationSummary { get; init; } = string.Empty;
    public string? CorrelationId { get; init; }
    public string SnapshotType { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyList<ConflictSnapshotItemDto> ConflictRecords { get; init; } =
        Array.Empty<ConflictSnapshotItemDto>();
    public IReadOnlyList<AuditTimelineSnapshotItemDto> AuditTimeline { get; init; } =
        Array.Empty<AuditTimelineSnapshotItemDto>();
    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Query-time alert signals at export generation (not persisted; not delivered externally).</summary>
    public IReadOnlyList<OperationalAlertSignalDto> AlertSignals { get; init; } =
        Array.Empty<OperationalAlertSignalDto>();

    public OperationalAlertSummaryDto? AlertSummary { get; init; }

    public string EscalationRisk { get; init; } = OperationalAlertSeverity.Info;

    public string OperationalPressureSummary { get; init; } = string.Empty;

    /// <summary>
    /// Compact derived summary (live scope counts + cached upstream pressure/risk).
    /// GOVERNANCE: advisory only; timeline/conflict/replay bodies in this DTO remain authoritative for the export scope.
    /// </summary>
    public OperationalForensicSnapshotSummaryDto? CompactSummary { get; init; }
}
