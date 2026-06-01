namespace Tannous.Pos.Application.Audit.Governance;

public sealed class OperationalGovernanceTelemetrySaturationDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public string SaturationLevel { get; init; } = string.Empty;
    public int ActiveTelemetryCategories { get; init; }
    public int ActiveScopedKeyCount { get; init; }
    public long TotalInvalidations { get; init; }
    public int ProjectionBreadthScore { get; init; }
    public int ExplainabilityDensityScore { get; init; }
    public long TelemetrySaturationEvents { get; init; }
    public IReadOnlyList<string> SaturationSignals { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> GovernanceNotes { get; init; } = Array.Empty<string>();
}
