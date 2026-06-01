namespace Tannous.Pos.Application.Audit.Governance;

public sealed class OperationalGovernanceSnapshotDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public OperationalGovernanceSnapshotMetadataDto Metadata { get; init; } = new();
    public OperationalGovernanceSnapshotFreshnessDto Freshness { get; init; } = new();
    public OperationalCacheGovernanceOverviewDto Overview { get; init; } = new();
    public OperationalCacheStabilityDto Stability { get; init; } = new();
    public OperationalCacheSurvivabilityDto Survivability { get; init; } = new();
    public OperationalDiagnosticsCacheDiagnosticsStaleRiskDto StaleRisk { get; init; } = new();
    public OperationalGovernanceRuntimeProtectionDto RuntimeProtection { get; init; } = new();
    public OperationalGovernanceTelemetrySaturationDto TelemetrySaturation { get; init; } = new();
    public OperationalCacheGovernanceConsistencyDto GovernanceConsistency { get; init; } = new();
    public string InvalidationPressureSeverity { get; init; } = string.Empty;
    public IReadOnlyList<string> ExplainabilityCodes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> GovernanceNotes { get; init; } = Array.Empty<string>();
}
