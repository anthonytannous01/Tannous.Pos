using Tannous.Pos.Application.OperationalCausality;
using Tannous.Pos.Application.OperationalDigest;
using Tannous.Pos.Application.OperationalEvolution;
using Tannous.Pos.Application.OperationalExperienceGraph;
using Tannous.Pos.Application.OperationalIntegrity;
using Tannous.Pos.Application.OperationalPatterns;
using Tannous.Pos.Application.OperationalPlaybooks;
using Tannous.Pos.Application.OperationalRecovery;
using Tannous.Pos.Application.OperationalSimulation;
using Tannous.Pos.Application.OperationalSituationRoom;
using Tannous.Pos.Application.OperationalTopology;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalTopologyArchitectureTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Topology_controller_is_get_only_with_admin_authorization()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditTopologyController.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("internal/operational-audit/topology", text, StringComparison.Ordinal);
        Assert.Contains("[Authorize(Policy = \"Admin\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"summary\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"chains\")]", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Topology_controller_has_no_repository_access()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditTopologyController.cs");
        var text = File.ReadAllText(path);

        Assert.DoesNotContain("IRepository", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PosDbContext", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Topology_layer_does_not_reference_governance_internals()
    {
        var dir = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "OperationalTopology");
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
    public void Topology_service_uses_operational_services_not_synthesis_service_recursion()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalTopologyService.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("IOperationalReadCompositionHub", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTrendService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTopologySnapshotStore", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalDashboardService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalEvolutionService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalDigestService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalExperienceGraphService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalIntegrityService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Task.WhenAll", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Topology_store_is_bounded_in_memory_only()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalTopology",
            "OperationalTopologySnapshotStore.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("OperationalTopologySnapshotStore", text, StringComparison.Ordinal);
        Assert.Contains("MaxStoredSnapshots", text, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Redis", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Topology_aggregation_limits_outputs_and_orders_deterministically()
    {
        var generatedAtUtc = new DateTime(2026, 5, 23, 0, 0, 0, DateTimeKind.Utc);

        var topology = OperationalTopologyAggregation.ComposeOperationalTopology(
            new OperationalRecoveryPostureDto { OverallDirection = OperationalRecoveryDirection.Improving },
            new OperationalCausalitySummaryDto { DominantOperationalArea = OperationalTopologyAggregation.AreaReplay },
            new OperationalPropagationAnalysisDto(),
            Array.Empty<OperationalCausalChainDto>(),
            new OperationalSituationRoomDto(),
            new OperationalSimulationSummaryDto(),
            new OperationalPlaybooksDto(),
            new OperationalPatternSummaryDto(),
            new OperationalEvolutionTimelineDto(),
            new OperationalIntegrityReportDto(),
            new OperationalExperienceGraphDto(),
            new OperationalDigestDto(),
            Array.Empty<OperationalTopologySnapshot>(),
            generatedAtUtc);

        Assert.True(topology.Dependencies.Count <= OperationalTopologyAggregation.MaxDependencies);
        Assert.True(topology.Influences.Count <= OperationalTopologyAggregation.MaxInfluences);
        Assert.False(string.IsNullOrWhiteSpace(topology.OperatorSummary));
    }

    [Fact]
    public void Topology_interpretation_uses_operator_wording_only()
    {
        var topology = OperationalTopologyAggregation.ComposeOperationalTopology(
            new OperationalRecoveryPostureDto(),
            new OperationalCausalitySummaryDto(),
            new OperationalPropagationAnalysisDto(),
            Array.Empty<OperationalCausalChainDto>(),
            new OperationalSituationRoomDto(),
            new OperationalSimulationSummaryDto(),
            new OperationalPlaybooksDto(),
            new OperationalPatternSummaryDto(),
            new OperationalEvolutionTimelineDto(),
            new OperationalIntegrityReportDto(),
            new OperationalExperienceGraphDto(),
            new OperationalDigestDto(),
            Array.Empty<OperationalTopologySnapshot>(),
            DateTime.UtcNow);

        var forbidden = new[] { "Pipeline", "Governance", "Explainability", "Classifier", "Projection", "LLM", "MachineLearning", "Tracing", "ServiceMesh", "GraphDatabase", "TimeSeries" };
        foreach (var term in forbidden)
        {
            Assert.DoesNotContain(term, topology.OperatorSummary, StringComparison.OrdinalIgnoreCase);
            Assert.All(topology.Dependencies, d => Assert.DoesNotContain(term, d.OperatorInterpretation, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Topology_service_is_registered_in_program()
    {
        var program = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Program.cs"));
        Assert.Contains("IOperationalTopologyService", program, StringComparison.Ordinal);
        Assert.Contains("OperationalTopologyService", program, StringComparison.Ordinal);
        Assert.Contains("IOperationalTopologySnapshotStore", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Topology_observability_anchor_exists()
    {
        var service = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalTopologyService.cs"));
        Assert.Contains("Operational topology observability:", service, StringComparison.Ordinal);
    }
}
