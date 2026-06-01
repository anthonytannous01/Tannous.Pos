namespace Tannous.Pos.Application.Audit;

public sealed class OperationalPressureLifecycleDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public string LifecycleState { get; init; } = string.Empty;
    public long ActiveEpoch { get; init; }
    public long LifecycleTransitions { get; init; }
    public long StabilizationCycles { get; init; }
    public bool QueryDateRangeClamped { get; init; }
    public bool QueryPageSizeClamped { get; init; }
    public bool ForensicExportTruncated { get; init; }
    public bool StickyPressureDetected { get; init; }
    public DateTime? LastResetUtc { get; init; }
    public DateTime? LastRecoveryUtc { get; init; }
    public IReadOnlyList<string> ReasonCodes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> GovernanceNotes { get; init; } = Array.Empty<string>();
}
