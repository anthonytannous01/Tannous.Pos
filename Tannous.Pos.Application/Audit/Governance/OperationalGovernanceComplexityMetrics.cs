using Tannous.Pos.Application.Audit.Governance.Modules;

namespace Tannous.Pos.Application.Audit.Governance;

/// <summary>Bounded governance complexity reporting (scan/architecture only; not runtime endpoints).</summary>
public static class OperationalGovernanceComplexityMetrics
{
    public const int MaxPipelineStageCount = 8;
    public const int MaxCollaboratorFanout = 8;
    public const int MaxModuleCouplingScore = 12;
    public const int MaxExplainabilityContributorCount = 24;

    public static OperationalGovernanceComplexityMeasurement Measure(
        int collaboratorFanout,
        int classifierReuseCount)
    {
        var graph = OperationalGovernanceModuleRegistry.DependencyGraph();
        var modules = OperationalGovernanceModuleRegistry.All;

        return new OperationalGovernanceComplexityMeasurement(
            ProjectionPipelineStageCount: OperationalGovernanceProjectionPipeline.StageOrder.Count,
            ModuleCount: modules.Count,
            CollaboratorFanout: collaboratorFanout,
            ExplainabilityContributorCount: modules.Sum(m => m.ExplainabilityContributorTypes.Count),
            ClassifierReuseCount: classifierReuseCount,
            ModuleCouplingScore: OperationalGovernanceDependencyRules.ComputeModuleCouplingScore(graph),
            ProjectionDepth: OperationalGovernanceProjectionPipeline.StageOrder.Count);
    }

    public sealed record OperationalGovernanceComplexityMeasurement(
        int ProjectionPipelineStageCount,
        int ModuleCount,
        int CollaboratorFanout,
        int ExplainabilityContributorCount,
        int ClassifierReuseCount,
        int ModuleCouplingScore,
        int ProjectionDepth)
    {
        public bool IsWithinBudget() =>
            ProjectionPipelineStageCount <= MaxPipelineStageCount
            && CollaboratorFanout <= MaxCollaboratorFanout
            && ModuleCouplingScore <= MaxModuleCouplingScore;
    }
}
