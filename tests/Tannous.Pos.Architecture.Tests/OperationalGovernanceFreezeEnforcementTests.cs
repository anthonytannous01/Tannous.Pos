using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Audit.Governance;
using Tannous.Pos.Application.Audit.Governance.Modules;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalGovernanceFreezeEnforcementTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Measured_surface_is_within_frozen_ceilings()
    {
        var snapshot = OperationalGovernanceSurfaceMeasurementHelper.MeasureFromRepository(RepoRoot());
        var validation = OperationalGovernanceExpansionGuard.Validate(snapshot);

        Assert.True(
            validation.IsFrozenCompliant,
            $"Governance freeze violated: {string.Join(", ", validation.Violations)}; "
            + $"endpoints={snapshot.CacheDiagnosticsGetEndpointCount}, "
            + $"collaborators={snapshot.ProjectionCollaboratorCount}, "
            + $"stages={snapshot.PipelineStageCount}, "
            + $"builders={snapshot.GovernanceProjectionBuilderCount}, "
            + $"classifiers={snapshot.GovernanceClassifierCount}, "
            + $"explainabilityBuilders={snapshot.GovernanceExplainabilityBuilderCount}, "
            + $"dtos={snapshot.GovernanceDiagnosticsDtoCount}, "
            + $"contributors={snapshot.ExplainabilityContributorCount}");
    }

    [Fact]
    public void Module_registry_count_is_frozen()
    {
        Assert.Equal(
            OperationalGovernanceFreezePolicy.FrozenModuleCount,
            OperationalGovernanceModuleRegistry.All.Count);
        Assert.True(OperationalGovernanceFreezePolicy.IsModuleCountFrozen(
            OperationalGovernanceModuleRegistry.All.Count));
    }

    [Fact]
    public void Pipeline_stage_count_is_frozen()
    {
        var stageCount = OperationalGovernanceProjectionPipeline.StageOrder.Count;
        Assert.Equal(OperationalGovernanceFreezePolicy.FrozenPipelineStageCount, stageCount);
        Assert.True(OperationalGovernanceFreezePolicy.IsPipelineStageCountFrozen(stageCount));
    }

    [Fact]
    public void Surface_audit_reports_freeze_and_dead_surface()
    {
        var report = OperationalGovernanceSurfaceAudit.Audit(RepoRoot());

        Assert.True(report.IsWithinBudget);
        Assert.True(report.IsFreezeCompliant);
        Assert.NotEmpty(report.FreezeRationale);
        Assert.NotEmpty(report.ApprovedExtensionPolicy);
        Assert.NotEmpty(report.OwnershipBoundaries);
    }

    [Fact]
    public void Dead_surface_detector_identifies_orphan_internal_service_methods()
    {
        var result = OperationalGovernanceDeadSurfaceDetector.Detect(RepoRoot());

        Assert.Contains(result.Findings, f => f.StartsWith("OrphanServiceMethod:", StringComparison.Ordinal));
    }

    [Fact]
    public void Expansion_guard_uses_budget_constants_not_literals()
    {
        var snapshot = OperationalGovernanceSurfaceMeasurementHelper.MeasureFromRepository(RepoRoot());
        var validation = OperationalGovernanceExpansionGuard.Validate(snapshot);

        Assert.Equal(snapshot, validation.Snapshot);
        Assert.True(validation.Snapshot.CacheDiagnosticsGetEndpointCount
            <= OperationalGovernanceSurfaceBudget.MaxCacheDiagnosticsGetEndpoints);
    }

    [Fact]
    public void Snapshot_store_avoids_service_provider_dependency()
    {
        var store = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalDiagnosticsProjections",
            "OperationalGovernanceSnapshotStore.cs"));

        Assert.DoesNotContain("IServiceProvider", store, StringComparison.Ordinal);
        Assert.Contains("Lazy<IOperationalDiagnosticsCache>", store, StringComparison.Ordinal);
    }

    [Fact]
    public void No_new_governance_get_endpoints_beyond_frozen_budget()
    {
        var snapshot = OperationalGovernanceSurfaceMeasurementHelper.MeasureFromRepository(RepoRoot());
        Assert.True(snapshot.CacheDiagnosticsGetEndpointCount <= OperationalGovernanceSurfaceBudget.MaxCacheDiagnosticsGetEndpoints);
    }
}
