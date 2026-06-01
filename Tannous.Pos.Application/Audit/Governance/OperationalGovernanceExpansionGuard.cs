namespace Tannous.Pos.Application.Audit.Governance;

/// <summary>Validates measured governance surface against frozen ceilings.</summary>
public static class OperationalGovernanceExpansionGuard
{
    public static OperationalGovernanceExpansionValidationResult Validate(
        OperationalGovernanceCeilingMeasurement.OperationalGovernanceCeilingSnapshot snapshot)
    {
        var violations = new List<string>();

        if (snapshot.CacheDiagnosticsGetEndpointCount > OperationalGovernanceSurfaceBudget.MaxCacheDiagnosticsGetEndpoints)
            violations.Add($"EndpointCount:{snapshot.CacheDiagnosticsGetEndpointCount}>{OperationalGovernanceSurfaceBudget.MaxCacheDiagnosticsGetEndpoints}");

        if (snapshot.ProjectionCollaboratorCount > OperationalGovernanceComplexityMetrics.MaxCollaboratorFanout)
            violations.Add($"CollaboratorFanout:{snapshot.ProjectionCollaboratorCount}>{OperationalGovernanceComplexityMetrics.MaxCollaboratorFanout}");

        if (snapshot.ProjectionCollaboratorCount > OperationalGovernanceRuntimeBudget.MaxProjectionCollaborators)
            violations.Add($"ProjectionCollaborators:{snapshot.ProjectionCollaboratorCount}>{OperationalGovernanceRuntimeBudget.MaxProjectionCollaborators}");

        if (snapshot.PipelineStageCount > OperationalGovernanceComplexityMetrics.MaxPipelineStageCount)
            violations.Add($"PipelineStages:{snapshot.PipelineStageCount}>{OperationalGovernanceComplexityMetrics.MaxPipelineStageCount}");

        if (!OperationalGovernanceFreezePolicy.IsPipelineStageCountFrozen(snapshot.PipelineStageCount))
            violations.Add($"PipelineStagesFrozen:{snapshot.PipelineStageCount}!={OperationalGovernanceFreezePolicy.FrozenPipelineStageCount}");

        if (snapshot.GovernanceProjectionBuilderCount > OperationalGovernanceSurfaceBudget.MaxGovernanceProjectionBuilderTypes)
            violations.Add($"ProjectionBuilders:{snapshot.GovernanceProjectionBuilderCount}>{OperationalGovernanceSurfaceBudget.MaxGovernanceProjectionBuilderTypes}");

        if (snapshot.GovernanceClassifierCount > OperationalGovernanceSurfaceBudget.MaxGovernanceClassifierTypes)
            violations.Add($"Classifiers:{snapshot.GovernanceClassifierCount}>{OperationalGovernanceSurfaceBudget.MaxGovernanceClassifierTypes}");

        if (snapshot.GovernanceExplainabilityBuilderCount > OperationalGovernanceSurfaceBudget.MaxGovernanceExplainabilityProfiles)
            violations.Add($"ExplainabilityBuilders:{snapshot.GovernanceExplainabilityBuilderCount}>{OperationalGovernanceSurfaceBudget.MaxGovernanceExplainabilityProfiles}");

        if (snapshot.GovernanceDiagnosticsDtoCount > OperationalGovernanceSurfaceBudget.MaxGovernanceDiagnosticsDtoTypes)
            violations.Add($"DiagnosticsDtos:{snapshot.GovernanceDiagnosticsDtoCount}>{OperationalGovernanceSurfaceBudget.MaxGovernanceDiagnosticsDtoTypes}");

        if (snapshot.ExplainabilityContributorCount > OperationalGovernanceComplexityMetrics.MaxExplainabilityContributorCount)
            violations.Add($"ExplainabilityContributors:{snapshot.ExplainabilityContributorCount}>{OperationalGovernanceComplexityMetrics.MaxExplainabilityContributorCount}");

        if (!OperationalGovernanceFreezePolicy.IsModuleCountFrozen(OperationalGovernanceFreezePolicy.RegistryModuleCount()))
            violations.Add($"ModuleCount:{OperationalGovernanceFreezePolicy.RegistryModuleCount()}!={OperationalGovernanceFreezePolicy.FrozenModuleCount}");

        return new OperationalGovernanceExpansionValidationResult(
            snapshot,
            violations,
            IsFrozenCompliant: violations.Count == 0 && snapshot.IsWithinBudget());
    }

    public sealed record OperationalGovernanceExpansionValidationResult(
        OperationalGovernanceCeilingMeasurement.OperationalGovernanceCeilingSnapshot Snapshot,
        IReadOnlyList<string> Violations,
        bool IsFrozenCompliant);
}
