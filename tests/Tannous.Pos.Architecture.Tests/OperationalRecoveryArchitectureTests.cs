using Tannous.Pos.Application.OperationalComposition;
using Tannous.Pos.Application.OperationalDashboard;
using Tannous.Pos.Application.OperationalInventoryWorkbench;
using Tannous.Pos.Application.OperationalReplayWorkbench;
using Tannous.Pos.Application.OperationalTimeline;
using Tannous.Pos.Application.OperationalTrends;
using Tannous.Pos.Application.OperationalTriage;
using Tannous.Pos.Application.OperationalRecovery;
using Tannous.Pos.Application.OperationalWorkbench;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalRecoveryArchitectureTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Recovery_controller_is_get_only_with_admin_authorization()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditRecoveryController.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("internal/operational-audit/recovery", text, StringComparison.Ordinal);
        Assert.Contains("[Authorize(Policy = \"Admin\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"outlook\")]", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Recovery_controller_has_no_repository_access()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditRecoveryController.cs");
        var text = File.ReadAllText(path);

        Assert.DoesNotContain("IRepository", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PosDbContext", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Recovery_layer_does_not_reference_governance_internals()
    {
        var dir = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "OperationalRecovery");
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
    public void Recovery_service_uses_operational_services_not_workbench_services()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalRecoveryService.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("IOperationalReadCompositionHub", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTrendService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTimelineService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTriageService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalDashboardService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalReconciliationWorkbenchService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalInventoryWorkbenchService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalReplayWorkbenchService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Task.WhenAll", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Recovery_aggregation_limits_signals_and_orders_deterministically()
    {
        var replayPressure = new OperationalReplayPressureSummaryDto
        {
            InstabilityLevel = OperationalReplayPressureLevel.Critical,
            Summary = "Replay instability requires investigation"
        };
        var replayStabilization = new OperationalReplayStabilizationDto
        {
            ReplayRecoveryImproving = true,
            ReplayPressureEscalating = true
        };
        var replayRecoveryConfidence = new OperationalReplayRecoveryConfidenceDto
        {
            Confidence = OperationalReplayRecoveryConfidence.Recovering,
            Summary = "Replay recovery confidence improving"
        };

        var posture = OperationalRecoveryAggregation.ComposePosture(
            new OperationalTrendSummaryDto { OverallDirection = OperationalTrendDirection.Improving },
            new OperationalTimelineDto { EventCount = 2, Summary = "Timeline activity observed" },
            Array.Empty<OperationalTimelineCorrelationDto>(),
            new OperationalTriageQueueDto { OverallPriority = OperationalTriagePriority.High },
            new OperationalDashboardSummaryDto(),
            replayPressure,
            replayStabilization,
            replayRecoveryConfidence,
            new OperationalReconciliationWorkbenchDto
            {
                Queue = new OperationalReconciliationQueueDto { EscalatingConflicts = 2 },
                ReplayRisk = new OperationalReconciliationReplayRiskDto { StabilizationRecovering = true }
            },
            new OperationalInventoryWorkbenchDto
            {
                DriftSummary = new OperationalInventoryDriftSummaryDto
                {
                    DriftSeverity = OperationalInventoryDriftSeverity.High,
                    EscalatingDriftConflicts = 1
                }
            },
            new OperationalGovernanceRuntimeProtectionSnapshot { FailsafeActive = true },
            new OperationalGovernanceFingerprintSnapshot { FingerprintStability = "Volatile", FingerprintChanged = true },
            runtimeSaturationIndicated: true,
            protectiveModeActive: true);

        Assert.True(posture.Signals.Count <= OperationalRecoveryAggregation.MaxSignals);
        Assert.True(posture.Recommendations.Count <= OperationalRecoveryAggregation.MaxRecommendations);
        Assert.True(posture.Attention.Count <= OperationalRecoveryAggregation.MaxAttentionItems);

        var postureRepeat = OperationalRecoveryAggregation.ComposePosture(
            new OperationalTrendSummaryDto { OverallDirection = OperationalTrendDirection.Improving },
            new OperationalTimelineDto { EventCount = 2, Summary = "Timeline activity observed" },
            Array.Empty<OperationalTimelineCorrelationDto>(),
            new OperationalTriageQueueDto { OverallPriority = OperationalTriagePriority.High },
            new OperationalDashboardSummaryDto(),
            replayPressure,
            replayStabilization,
            replayRecoveryConfidence,
            new OperationalReconciliationWorkbenchDto
            {
                Queue = new OperationalReconciliationQueueDto { EscalatingConflicts = 2 },
                ReplayRisk = new OperationalReconciliationReplayRiskDto { StabilizationRecovering = true }
            },
            new OperationalInventoryWorkbenchDto
            {
                DriftSummary = new OperationalInventoryDriftSummaryDto
                {
                    DriftSeverity = OperationalInventoryDriftSeverity.High,
                    EscalatingDriftConflicts = 1
                }
            },
            new OperationalGovernanceRuntimeProtectionSnapshot { FailsafeActive = true },
            new OperationalGovernanceFingerprintSnapshot { FingerprintStability = "Volatile", FingerprintChanged = true },
            runtimeSaturationIndicated: true,
            protectiveModeActive: true);

        Assert.Equal(
            posture.Signals.Select(s => s.SignalId),
            postureRepeat.Signals.Select(s => s.SignalId));
    }

    [Fact]
    public void Recovery_outlook_has_five_bounded_sections()
    {
        var outlook = OperationalRecoveryAggregation.ComposeOutlook(
            new OperationalTrendSummaryDto { OverallDirection = OperationalTrendDirection.Stable },
            new OperationalTimelineDto(),
            new OperationalTriageQueueDto(),
            new OperationalDashboardSummaryDto { ReadinessSummary = "Operational readiness stable" },
            new OperationalReplayPressureSummaryDto(),
            new OperationalReplayStabilizationDto { ReplayRecoveryImproving = true },
            new OperationalReplayRecoveryConfidenceDto { Confidence = OperationalReplayRecoveryConfidence.Recovering },
            new OperationalReconciliationWorkbenchDto(),
            new OperationalInventoryWorkbenchDto(),
            new OperationalGovernanceRuntimeProtectionSnapshot(),
            new OperationalGovernanceFingerprintSnapshot(),
            runtimeSaturationIndicated: false,
            protectiveModeActive: false);

        Assert.Equal(5, outlook.SectionCount);
        Assert.True(outlook.Convergence.Count <= OperationalRecoveryAggregation.MaxConvergenceItems);
        Assert.Contains(outlook.Sections, s => s.SectionId == OperationalRecoveryAggregation.SectionReplayRecovery);
        Assert.Contains(outlook.Sections, s => s.SectionId == OperationalRecoveryAggregation.SectionOperationalStability);
    }

    [Fact]
    public void Recovery_classifies_convergence_and_direction_transitions()
    {
        var improving = OperationalRecoveryAggregation.ComposePosture(
            new OperationalTrendSummaryDto { OverallDirection = OperationalTrendDirection.Improving },
            new OperationalTimelineDto(),
            Array.Empty<OperationalTimelineCorrelationDto>(),
            new OperationalTriageQueueDto(),
            new OperationalDashboardSummaryDto(),
            new OperationalReplayPressureSummaryDto(),
            new OperationalReplayStabilizationDto { ReplayRecoveryImproving = true },
            new OperationalReplayRecoveryConfidenceDto { Confidence = OperationalReplayRecoveryConfidence.Recovering },
            new OperationalReconciliationWorkbenchDto { ReplayRisk = new OperationalReconciliationReplayRiskDto { StabilizationRecovering = true } },
            new OperationalInventoryWorkbenchDto(),
            new OperationalGovernanceRuntimeProtectionSnapshot(),
            new OperationalGovernanceFingerprintSnapshot { FingerprintStability = "Stable" },
            runtimeSaturationIndicated: false,
            protectiveModeActive: false);

        var degrading = OperationalRecoveryAggregation.ComposePosture(
            new OperationalTrendSummaryDto { OverallDirection = OperationalTrendDirection.Degrading },
            new OperationalTimelineDto(),
            Array.Empty<OperationalTimelineCorrelationDto>(),
            new OperationalTriageQueueDto { OverallPriority = OperationalTriagePriority.Moderate },
            new OperationalDashboardSummaryDto(),
            new OperationalReplayPressureSummaryDto { InstabilityLevel = OperationalReplayPressureLevel.High },
            new OperationalReplayStabilizationDto { ReplayPressureEscalating = true },
            new OperationalReplayRecoveryConfidenceDto { Confidence = OperationalReplayRecoveryConfidence.Fragile },
            new OperationalReconciliationWorkbenchDto { Queue = new OperationalReconciliationQueueDto { EscalatingConflicts = 4 } },
            new OperationalInventoryWorkbenchDto { DriftSummary = new OperationalInventoryDriftSummaryDto { EscalatingDriftConflicts = 2 } },
            new OperationalGovernanceRuntimeProtectionSnapshot(),
            new OperationalGovernanceFingerprintSnapshot { FingerprintStability = "Stable", FingerprintChanged = false },
            runtimeSaturationIndicated: false,
            protectiveModeActive: false);

        Assert.Equal(OperationalRecoveryDirection.Improving, improving.OverallDirection);
        Assert.Contains(improving.Convergence, c => c.Direction == OperationalRecoveryDirection.Converging);
        Assert.Equal(OperationalRecoveryState.Degrading, degrading.OverallState);
        Assert.Contains(degrading.Convergence, c => c.Direction == OperationalRecoveryDirection.Diverging);
    }

    [Fact]
    public void Recovery_classifies_saturated_and_volatile_states()
    {
        var saturated = OperationalRecoveryAggregation.ComposePosture(
            new OperationalTrendSummaryDto { OverallDirection = OperationalTrendDirection.Degrading },
            new OperationalTimelineDto(),
            Array.Empty<OperationalTimelineCorrelationDto>(),
            new OperationalTriageQueueDto { OverallPriority = OperationalTriagePriority.Critical },
            new OperationalDashboardSummaryDto(),
            new OperationalReplayPressureSummaryDto { InstabilityLevel = OperationalReplayPressureLevel.Critical },
            new OperationalReplayStabilizationDto { ReplayPressureEscalating = true },
            new OperationalReplayRecoveryConfidenceDto { Confidence = OperationalReplayRecoveryConfidence.Fragile },
            new OperationalReconciliationWorkbenchDto(),
            new OperationalInventoryWorkbenchDto(),
            new OperationalGovernanceRuntimeProtectionSnapshot(),
            new OperationalGovernanceFingerprintSnapshot { FingerprintStability = "Volatile", FingerprintChanged = true },
            runtimeSaturationIndicated: true,
            protectiveModeActive: true);

        Assert.Equal(OperationalRecoveryState.Saturated, saturated.OverallState);
        Assert.Equal(OperationalRecoveryState.Volatile, OperationalRecoveryAggregation.ComposePosture(
            new OperationalTrendSummaryDto { OverallDirection = OperationalTrendDirection.Degrading },
            new OperationalTimelineDto(),
            Array.Empty<OperationalTimelineCorrelationDto>(),
            new OperationalTriageQueueDto { OverallPriority = OperationalTriagePriority.High },
            new OperationalDashboardSummaryDto(),
            new OperationalReplayPressureSummaryDto(),
            new OperationalReplayStabilizationDto(),
            new OperationalReplayRecoveryConfidenceDto(),
            new OperationalReconciliationWorkbenchDto(),
            new OperationalInventoryWorkbenchDto(),
            new OperationalGovernanceRuntimeProtectionSnapshot(),
            new OperationalGovernanceFingerprintSnapshot { FingerprintStability = "Volatile", FingerprintChanged = true },
            runtimeSaturationIndicated: false,
            protectiveModeActive: false).OverallState);
    }

    [Fact]
    public void Recovery_routes_use_existing_operational_paths_only()
    {
        var posture = OperationalRecoveryAggregation.ComposePosture(
            new OperationalTrendSummaryDto(),
            new OperationalTimelineDto { EventCount = 1, Summary = "Event" },
            Array.Empty<OperationalTimelineCorrelationDto>(),
            new OperationalTriageQueueDto(),
            new OperationalDashboardSummaryDto(),
            new OperationalReplayPressureSummaryDto { InstabilityLevel = OperationalReplayPressureLevel.High },
            new OperationalReplayStabilizationDto(),
            new OperationalReplayRecoveryConfidenceDto(),
            new OperationalReconciliationWorkbenchDto(),
            new OperationalInventoryWorkbenchDto(),
            new OperationalGovernanceRuntimeProtectionSnapshot(),
            new OperationalGovernanceFingerprintSnapshot { FingerprintChanged = true },
            runtimeSaturationIndicated: false,
            protectiveModeActive: false);

        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            OperationalRecoveryAggregation.RouteDashboard,
            OperationalRecoveryAggregation.RouteReconciliationWorkbench,
            OperationalRecoveryAggregation.RouteInventoryWorkbench,
            OperationalRecoveryAggregation.RouteReplayWorkbench,
            OperationalRecoveryAggregation.RouteTrendSummary,
            OperationalRecoveryAggregation.RouteTimeline,
            OperationalRecoveryAggregation.RouteTriage,
            OperationalRecoveryAggregation.RouteNavigation
        };

        Assert.All(posture.Signals, s => Assert.Contains(s.RecommendedRoute, allowed));
        Assert.All(posture.Recommendations, r => Assert.Contains(r.RecommendedRoute, allowed));
    }

    [Fact]
    public void Recovery_summary_uses_operator_wording_only()
    {
        var posture = OperationalRecoveryAggregation.ComposePosture(
            new OperationalTrendSummaryDto { OverallDirection = OperationalTrendDirection.Improving, Summary = "Trend improving" },
            new OperationalTimelineDto(),
            Array.Empty<OperationalTimelineCorrelationDto>(),
            new OperationalTriageQueueDto(),
            new OperationalDashboardSummaryDto(),
            new OperationalReplayPressureSummaryDto(),
            new OperationalReplayStabilizationDto { ReplayRecoveryImproving = true },
            new OperationalReplayRecoveryConfidenceDto { Summary = "Recovery improving" },
            new OperationalReconciliationWorkbenchDto(),
            new OperationalInventoryWorkbenchDto(),
            new OperationalGovernanceRuntimeProtectionSnapshot(),
            new OperationalGovernanceFingerprintSnapshot(),
            runtimeSaturationIndicated: false,
            protectiveModeActive: false);

        var forbidden = new[] { "Pipeline", "Governance", "Explainability", "Classifier", "Projection" };
        foreach (var term in forbidden)
        {
            Assert.DoesNotContain(term, posture.Summary, StringComparison.OrdinalIgnoreCase);
            Assert.All(posture.Signals, s => Assert.DoesNotContain(term, s.Summary, StringComparison.OrdinalIgnoreCase));
            Assert.All(posture.Recommendations, r => Assert.DoesNotContain(term, r.Summary, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Recovery_service_is_registered_in_program()
    {
        var program = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Program.cs"));
        Assert.Contains("IOperationalRecoveryService", program, StringComparison.Ordinal);
        Assert.Contains("OperationalRecoveryService", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Recovery_observability_anchor_exists()
    {
        var service = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalRecoveryService.cs"));
        Assert.Contains("Operational recovery observability:", service, StringComparison.Ordinal);
    }
}
