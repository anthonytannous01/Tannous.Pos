using Tannous.Pos.Application.OperationalComposition;
using Tannous.Pos.Application.OperationalCausality;
using Tannous.Pos.Application.OperationalIncidents;
using Tannous.Pos.Application.OperationalInventoryWorkbench;
using Tannous.Pos.Application.OperationalRecovery;
using Tannous.Pos.Application.OperationalReplayWorkbench;
using Tannous.Pos.Application.OperationalTimeline;
using Tannous.Pos.Application.OperationalTrends;
using Tannous.Pos.Application.OperationalTriage;
using Tannous.Pos.Application.OperationalWorkbench;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalCausalityArchitectureTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Causality_controller_is_get_only_with_admin_authorization()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditCausalityController.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("internal/operational-audit/causality", text, StringComparison.Ordinal);
        Assert.Contains("[Authorize(Policy = \"Admin\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"summary\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"propagation\")]", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Causality_controller_has_no_repository_access()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditCausalityController.cs");
        var text = File.ReadAllText(path);

        Assert.DoesNotContain("IRepository", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PosDbContext", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Causality_layer_does_not_reference_governance_internals()
    {
        var dir = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "OperationalCausality");
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
    public void Causality_service_uses_operational_services_not_workbench_services()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalCausalityService.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("IOperationalReadCompositionHub", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTrendService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTimelineService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTriageService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalRecoveryService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalCausalitySnapshotStore", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalDashboardService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalReconciliationWorkbenchService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalInventoryWorkbenchService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalReplayWorkbenchService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalIncidentService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Task.WhenAll", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Causality_store_is_bounded_in_memory_only()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalCausality",
            "OperationalCausalitySnapshotStore.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("OperationalCausalitySnapshotStore", text, StringComparison.Ordinal);
        Assert.Contains("MaxStoredSnapshots", text, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Redis", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Causality_aggregation_limits_chains_and_orders_deterministically()
    {
        var generatedAtUtc = new DateTime(2026, 5, 21, 14, 0, 0, DateTimeKind.Utc);
        var recovery = new OperationalRecoveryPostureDto
        {
            OverallState = OperationalRecoveryState.Degrading,
            Summary = "Operational conditions degrading"
        };

        var chains = OperationalCausalityAggregation.ComposeChains(
            new OperationalTrendSummaryDto { OverallDirection = OperationalTrendDirection.Degrading },
            new OperationalTimelineDto(),
            Array.Empty<OperationalTimelineCorrelationDto>(),
            new OperationalTriageQueueDto { OverallPriority = OperationalTriagePriority.High },
            recovery,
            Array.Empty<OperationalIncidentCaseDto>(),
            new OperationalReplayPressureSummaryDto { InstabilityLevel = OperationalReplayPressureLevel.Critical },
            new OperationalReplayStabilizationDto { ReplayPressureEscalating = true },
            new OperationalReconciliationWorkbenchDto { Queue = new OperationalReconciliationQueueDto { EscalatingConflicts = 2 } },
            new OperationalInventoryWorkbenchDto(),
            new OperationalGovernanceRuntimeProtectionSnapshot(),
            new OperationalGovernanceFingerprintSnapshot(),
            runtimeSaturationIndicated: true,
            protectiveModeActive: true,
            priorSnapshots: Array.Empty<OperationalCausalitySnapshot>(),
            generatedAtUtc);

        Assert.True(chains.ChainCount <= OperationalCausalityAggregation.MaxCausalChains);
        Assert.True(chains.Nodes.Count <= OperationalCausalityAggregation.MaxCausalChains * OperationalCausalityAggregation.MaxNodesPerChain);

        var repeat = OperationalCausalityAggregation.ComposeChains(
            new OperationalTrendSummaryDto { OverallDirection = OperationalTrendDirection.Degrading },
            new OperationalTimelineDto(),
            Array.Empty<OperationalTimelineCorrelationDto>(),
            new OperationalTriageQueueDto { OverallPriority = OperationalTriagePriority.High },
            recovery,
            Array.Empty<OperationalIncidentCaseDto>(),
            new OperationalReplayPressureSummaryDto { InstabilityLevel = OperationalReplayPressureLevel.Critical },
            new OperationalReplayStabilizationDto { ReplayPressureEscalating = true },
            new OperationalReconciliationWorkbenchDto { Queue = new OperationalReconciliationQueueDto { EscalatingConflicts = 2 } },
            new OperationalInventoryWorkbenchDto(),
            new OperationalGovernanceRuntimeProtectionSnapshot(),
            new OperationalGovernanceFingerprintSnapshot(),
            runtimeSaturationIndicated: true,
            protectiveModeActive: true,
            priorSnapshots: Array.Empty<OperationalCausalitySnapshot>(),
            generatedAtUtc);

        Assert.Equal(chains.Chains.Select(c => c.ChainId), repeat.Chains.Select(c => c.ChainId));
    }

    [Fact]
    public void Causality_propagation_detects_expanding_and_collapsing_pressure()
    {
        var analysis = OperationalCausalityAggregation.ComposePropagationAnalysis(
            new OperationalTrendSummaryDto { OverallDirection = OperationalTrendDirection.Improving },
            new OperationalTimelineDto(),
            Array.Empty<OperationalTimelineCorrelationDto>(),
            new OperationalTriageQueueDto(),
            new OperationalRecoveryPostureDto
            {
                Convergence = new[]
                {
                    new OperationalRecoveryConvergenceDto
                    {
                        Domain = "Replay",
                        Direction = OperationalRecoveryDirection.Converging,
                        Summary = "Replay pressure stabilizing"
                    }
                }
            },
            Array.Empty<OperationalIncidentCaseDto>(),
            new OperationalReplayPressureSummaryDto { InstabilityLevel = OperationalReplayPressureLevel.High },
            new OperationalReplayStabilizationDto { ReplayPressureEscalating = true, ReplayRecoveryImproving = false },
            new OperationalReconciliationWorkbenchDto { Queue = new OperationalReconciliationQueueDto { EscalatingConflicts = 1 } },
            new OperationalInventoryWorkbenchDto(),
            new OperationalGovernanceRuntimeProtectionSnapshot(),
            new OperationalGovernanceFingerprintSnapshot(),
            runtimeSaturationIndicated: true,
            protectiveModeActive: false);

        Assert.True(analysis.PropagationCount <= OperationalCausalityAggregation.MaxPropagations);
        Assert.True(analysis.RootCauseCandidateCount <= OperationalCausalityAggregation.MaxRootCauseCandidates);
        Assert.True(analysis.StabilizationBlockerCount <= OperationalCausalityAggregation.MaxStabilizationBlockers);
        Assert.Contains(analysis.Propagations, p => p.IsEscalating);
        Assert.Contains(analysis.Propagations, p => p.IsCollapsing);
    }

    [Fact]
    public void Causality_summary_uses_operator_wording_only()
    {
        var chains = OperationalCausalityAggregation.ComposeChains(
            new OperationalTrendSummaryDto(),
            new OperationalTimelineDto(),
            Array.Empty<OperationalTimelineCorrelationDto>(),
            new OperationalTriageQueueDto(),
            new OperationalRecoveryPostureDto { Summary = "Operational recovery confidence improving" },
            Array.Empty<OperationalIncidentCaseDto>(),
            new OperationalReplayPressureSummaryDto(),
            new OperationalReplayStabilizationDto(),
            new OperationalReconciliationWorkbenchDto(),
            new OperationalInventoryWorkbenchDto(),
            new OperationalGovernanceRuntimeProtectionSnapshot(),
            new OperationalGovernanceFingerprintSnapshot(),
            runtimeSaturationIndicated: false,
            protectiveModeActive: false,
            priorSnapshots: Array.Empty<OperationalCausalitySnapshot>(),
            DateTime.UtcNow);

        var propagation = OperationalCausalityAggregation.ComposePropagationAnalysis(
            new OperationalTrendSummaryDto(),
            new OperationalTimelineDto(),
            Array.Empty<OperationalTimelineCorrelationDto>(),
            new OperationalTriageQueueDto(),
            new OperationalRecoveryPostureDto(),
            Array.Empty<OperationalIncidentCaseDto>(),
            new OperationalReplayPressureSummaryDto(),
            new OperationalReplayStabilizationDto(),
            new OperationalReconciliationWorkbenchDto(),
            new OperationalInventoryWorkbenchDto(),
            new OperationalGovernanceRuntimeProtectionSnapshot(),
            new OperationalGovernanceFingerprintSnapshot(),
            runtimeSaturationIndicated: false,
            protectiveModeActive: false);

        var summary = OperationalCausalityAggregation.ComposeSummary(
            chains,
            propagation,
            new OperationalRecoveryPostureDto { Summary = "Recovery improving" },
            DateTime.UtcNow);

        var forbidden = new[] { "Pipeline", "Governance", "Explainability", "Classifier", "Projection", "Graph" };
        foreach (var term in forbidden)
        {
            Assert.DoesNotContain(term, summary.Summary, StringComparison.OrdinalIgnoreCase);
            Assert.All(propagation.RootCauseCandidates, r => Assert.DoesNotContain(term, r.Explanation, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Causality_service_is_registered_in_program()
    {
        var program = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Program.cs"));
        Assert.Contains("IOperationalCausalityService", program, StringComparison.Ordinal);
        Assert.Contains("OperationalCausalityService", program, StringComparison.Ordinal);
        Assert.Contains("IOperationalCausalitySnapshotStore", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Causality_observability_anchor_exists()
    {
        var service = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalCausalityService.cs"));
        Assert.Contains("Operational causality observability:", service, StringComparison.Ordinal);
    }
}
