using Tannous.Pos.Application.Audit.Governance.Modules;

namespace Tannous.Pos.Application.Audit.Governance;

/// <summary>Measured governance architecture ceilings for architecture tests and scans.</summary>
public static class OperationalGovernanceCeilingMeasurement
{
    public static OperationalGovernanceCeilingSnapshot Measure(
        int cacheDiagnosticsGetEndpointCount,
        int projectionCollaboratorCount,
        int governanceProjectionBuilderCount,
        int governanceClassifierCount,
        int governanceExplainabilityBuilderCount,
        int governanceDiagnosticsDtoCount)
    {
        var modules = OperationalGovernanceModuleRegistry.All;

        return new OperationalGovernanceCeilingSnapshot(
            CacheDiagnosticsGetEndpointCount: cacheDiagnosticsGetEndpointCount,
            ProjectionCollaboratorCount: projectionCollaboratorCount,
            PipelineStageCount: OperationalGovernanceProjectionPipeline.StageOrder.Count,
            GovernanceProjectionBuilderCount: governanceProjectionBuilderCount,
            GovernanceClassifierCount: governanceClassifierCount,
            GovernanceExplainabilityBuilderCount: governanceExplainabilityBuilderCount,
            GovernanceDiagnosticsDtoCount: governanceDiagnosticsDtoCount,
            ExplainabilityContributorCount: modules.Sum(m => m.ExplainabilityContributorTypes.Count));
    }

    public sealed record OperationalGovernanceCeilingSnapshot(
        int CacheDiagnosticsGetEndpointCount,
        int ProjectionCollaboratorCount,
        int PipelineStageCount,
        int GovernanceProjectionBuilderCount,
        int GovernanceClassifierCount,
        int GovernanceExplainabilityBuilderCount,
        int GovernanceDiagnosticsDtoCount,
        int ExplainabilityContributorCount)
    {
        public bool IsWithinBudget() =>
            CacheDiagnosticsGetEndpointCount <= OperationalGovernanceSurfaceBudget.MaxCacheDiagnosticsGetEndpoints
            && ProjectionCollaboratorCount <= OperationalGovernanceComplexityMetrics.MaxCollaboratorFanout
            && ProjectionCollaboratorCount <= OperationalGovernanceRuntimeBudget.MaxProjectionCollaborators
            && PipelineStageCount <= OperationalGovernanceComplexityMetrics.MaxPipelineStageCount
            && PipelineStageCount <= OperationalGovernanceRuntimeBudget.MaxPipelineDepth
            && GovernanceProjectionBuilderCount <= OperationalGovernanceSurfaceBudget.MaxGovernanceProjectionBuilderTypes
            && GovernanceClassifierCount <= OperationalGovernanceSurfaceBudget.MaxGovernanceClassifierTypes
            && GovernanceExplainabilityBuilderCount <= OperationalGovernanceSurfaceBudget.MaxGovernanceExplainabilityProfiles
            && GovernanceDiagnosticsDtoCount <= OperationalGovernanceSurfaceBudget.MaxGovernanceDiagnosticsDtoTypes
            && ExplainabilityContributorCount <= OperationalGovernanceComplexityMetrics.MaxExplainabilityContributorCount;
    }
}
