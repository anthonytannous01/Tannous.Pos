using Tannous.Pos.Application.OperationalAttention;
using Tannous.Pos.Application.OperationalConvergence;
using Tannous.Pos.Application.OperationalEvolution;
using Tannous.Pos.Application.OperationalIntegrity;
using Tannous.Pos.Application.OperationalRecovery;
using Tannous.Pos.Application.OperationalResilience;
using Tannous.Pos.Application.OperationalSituationRoom;
using Tannous.Pos.Application.OperationalStrategy;
using Tannous.Pos.Application.OperationalEquilibrium;
using Tannous.Pos.Application.OperationalTopology;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalEquilibriumArchitectureTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Equilibrium_controller_includes_endpoints_with_admin_authorization()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditEquilibriumController.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("internal/operational-audit/equilibrium", text, StringComparison.Ordinal);
        Assert.Contains("[Authorize(Policy = \"Admin\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"summary\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"imbalances\")]", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Equilibrium_service_uses_operational_services_not_synthesis_service_recursion()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalEquilibriumService.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("IOperationalReadCompositionHub", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTrendService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalEquilibriumSnapshotStore", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalDashboardService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalStrategyService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Task.WhenAll", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Equilibrium_layer_does_not_reference_governance_internals()
    {
        var dir = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "OperationalEquilibrium");
        var files = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories);

        Assert.NotEmpty(files);
        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("using Tannous.Pos.Application.Audit.Governance", text, StringComparison.Ordinal);
            Assert.DoesNotContain("OperationalGovernanceProjectionPipeline", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ExplainabilityComposer", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Equilibrium_store_is_bounded_in_memory_only()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalEquilibrium",
            "OperationalEquilibriumSnapshotStore.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("OperationalEquilibriumSnapshotStore", text, StringComparison.Ordinal);
        Assert.Contains("MaxStoredSnapshots", text, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Redis", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Equilibrium_aggregation_limits_outputs_and_orders_deterministically()
    {
        var report = OperationalEquilibriumAggregation.ComposeEquilibriumReport(
            new OperationalRecoveryPostureDto { OverallDirection = OperationalRecoveryDirection.Improving },
            new OperationalSituationRoomDto(),
            new OperationalEvolutionTimelineDto(),
            new OperationalTopologyDto(),
            new OperationalConvergenceReportDto { ConvergenceStrength = OperationalConvergenceStrength.Strong },
            new OperationalResilienceReportDto(),
            new OperationalAttentionReportDto(),
            new OperationalStrategyReportDto(),
            new OperationalIntegrityReportDto(),
            Array.Empty<OperationalFragilityDto>(),
            Array.Empty<OperationalEquilibriumSnapshot>(),
            new DateTime(2026, 5, 24, 0, 0, 0, DateTimeKind.Utc));

        Assert.True(report.SystemicBalances.Count <= OperationalEquilibriumAggregation.MaxSystemicBalances);
        Assert.True(report.Imbalances.Count <= OperationalEquilibriumAggregation.MaxImbalances);
        Assert.True(report.PressureDistributions.Count <= OperationalEquilibriumAggregation.MaxPressureDistributions);
        Assert.False(string.IsNullOrWhiteSpace(report.OperatorSummary));
    }

    [Fact]
    public void Equilibrium_interpretation_avoids_optimization_and_probabilistic_terminology()
    {
        var report = OperationalEquilibriumAggregation.ComposeEquilibriumReport(
            new OperationalRecoveryPostureDto(),
            new OperationalSituationRoomDto(),
            new OperationalEvolutionTimelineDto(),
            new OperationalTopologyDto(),
            new OperationalConvergenceReportDto(),
            new OperationalResilienceReportDto(),
            new OperationalAttentionReportDto(),
            new OperationalStrategyReportDto(),
            new OperationalIntegrityReportDto(),
            Array.Empty<OperationalFragilityDto>(),
            Array.Empty<OperationalEquilibriumSnapshot>(),
            DateTime.UtcNow);

        var forbidden = new[] { "Pipeline", "Governance", "Explainability", "Optimization", "Probabilistic", "MachineLearning", "ControlTheory", "Simulation" };
        foreach (var term in forbidden)
        {
            Assert.DoesNotContain(term, report.OperatorSummary, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Equilibrium_service_is_registered_in_program()
    {
        var program = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Program.cs"));
        Assert.Contains("IOperationalEquilibriumService", program, StringComparison.Ordinal);
        Assert.Contains("OperationalEquilibriumService", program, StringComparison.Ordinal);
        Assert.Contains("IOperationalEquilibriumSnapshotStore", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Equilibrium_observability_anchor_exists()
    {
        var service = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalEquilibriumService.cs"));
        Assert.Contains("Operational equilibrium observability:", service, StringComparison.Ordinal);
    }
}
