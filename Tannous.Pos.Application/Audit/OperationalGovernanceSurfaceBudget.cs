namespace Tannous.Pos.Application.Audit;

/// <summary>
/// Bounded governance surface budgeting (reporting + architecture guardrails).
/// GOVERNANCE: thresholds are intentional caps; raise only with consolidation review.
/// </summary>
public static class OperationalGovernanceSurfaceBudget
{
    public const int MaxCacheDiagnosticsGetEndpoints = 35;
    public const int MaxGovernanceProjectionBuilderTypes = 15;
    public const int MaxGovernanceExplainabilityProfiles = 6;
    public const int MaxGovernanceClassifierTypes = 30;
    public const int MaxGovernanceDiagnosticsDtoTypes = 75;
    public const int MaxExplainabilityItemsPerProjection = 8;
    public const int MaxExplainabilityCodeLength = 48;

    public static GovernanceSurfaceMeasurement MeasureFromSources(
        int cacheDiagnosticsGetEndpointCount,
        int governanceProjectionBuilderCount,
        int governanceExplainabilityBuilderCount,
        int governanceClassifierCount,
        int governanceDiagnosticsDtoCount)
    {
        return new GovernanceSurfaceMeasurement(
            cacheDiagnosticsGetEndpointCount,
            governanceProjectionBuilderCount,
            governanceExplainabilityBuilderCount,
            governanceClassifierCount,
            governanceDiagnosticsDtoCount);
    }

    public sealed record GovernanceSurfaceMeasurement(
        int CacheDiagnosticsGetEndpointCount,
        int GovernanceProjectionBuilderCount,
        int GovernanceExplainabilityBuilderCount,
        int GovernanceClassifierCount,
        int GovernanceDiagnosticsDtoCount)
    {
        public bool IsWithinBudget() =>
            CacheDiagnosticsGetEndpointCount <= MaxCacheDiagnosticsGetEndpoints
            && GovernanceProjectionBuilderCount <= MaxGovernanceProjectionBuilderTypes
            && GovernanceExplainabilityBuilderCount <= MaxGovernanceExplainabilityProfiles
            && GovernanceClassifierCount <= MaxGovernanceClassifierTypes
            && GovernanceDiagnosticsDtoCount <= MaxGovernanceDiagnosticsDtoTypes;
    }
}
