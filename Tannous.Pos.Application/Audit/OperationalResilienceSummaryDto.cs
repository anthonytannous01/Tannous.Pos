namespace Tannous.Pos.Application.Audit;

public sealed class OperationalResilienceSummaryDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public string PrimaryDegradedMode { get; init; } = OperationalDegradedModeTypes.Normal;
    public IReadOnlyList<string> ActiveDegradedModes { get; init; } = Array.Empty<string>();
    public string ReconciliationBacklogSeverity { get; init; } = "Normal";
    public bool QueryPressureIndicated { get; init; }
    public bool ReplayStormRiskIndicated { get; init; }
    public bool ExportTruncationPressureIndicated { get; init; }
    public bool AuditPersistencePressureIndicated { get; init; }
    public int UnresolvedConflictCount { get; init; }
    public int ReplayReceiptCount { get; init; }
    public int AuditRecordCount { get; init; }
    public int RecentAuditPersistenceFailures { get; init; }
    public IReadOnlyDictionary<string, string> ResilienceGuidance { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
