using Tannous.Pos.Application.OperationalComposition;
using Tannous.Pos.Application.OperationalDashboard;
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

public class OperationalIncidentArchitectureTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Incident_controller_is_get_only_with_admin_authorization()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditIncidentCasesController.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("internal/operational-audit/incident-cases", text, StringComparison.Ordinal);
        Assert.Contains("[Authorize(Policy = \"Admin\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"summary\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"{incidentId}\")]", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPut(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPatch(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpDelete(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Incident_controller_has_no_repository_access()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditIncidentCasesController.cs");
        var text = File.ReadAllText(path);

        Assert.DoesNotContain("IRepository", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PosDbContext", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Incident_layer_does_not_reference_governance_internals()
    {
        var dir = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "OperationalIncidents");
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
    public void Incident_service_uses_operational_services_not_workbench_services()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalIncidentService.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("IOperationalReadCompositionHub", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTrendService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTimelineService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalTriageService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalRecoveryService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalIncidentCaseStore", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalDashboardService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalReconciliationWorkbenchService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalInventoryWorkbenchService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalReplayWorkbenchService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Task.WhenAll", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Incident_store_is_bounded_in_memory_only()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalIncidents",
            "OperationalIncidentCaseStore.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("OperationalIncidentCaseStore", text, StringComparison.Ordinal);
        Assert.Contains("MaxStoredSnapshots", text, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Redis", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Incident_aggregation_limits_cases_and_orders_deterministically()
    {
        var generatedAtUtc = new DateTime(2026, 5, 21, 12, 0, 0, DateTimeKind.Utc);
        var replayPressure = new OperationalReplayPressureSummaryDto
        {
            InstabilityLevel = OperationalReplayPressureLevel.Critical,
            Summary = "Replay instability requires investigation"
        };

        var cases = OperationalIncidentAggregation.ComposeCases(
            new OperationalTrendSummaryDto { OverallDirection = OperationalTrendDirection.Degrading },
            new OperationalTimelineDto { EventCount = 1, Summary = "Timeline activity" },
            Array.Empty<OperationalTimelineCorrelationDto>(),
            new OperationalTriageQueueDto { OverallPriority = OperationalTriagePriority.High },
            new OperationalRecoveryPostureDto { OverallState = OperationalRecoveryState.Volatile },
            new OperationalDashboardSummaryDto(),
            replayPressure,
            new OperationalReplayStabilizationDto { ReplayPressureEscalating = true },
            new OperationalReplayRecoveryConfidenceDto { Confidence = OperationalReplayRecoveryConfidence.Fragile },
            new OperationalReconciliationWorkbenchDto { Queue = new OperationalReconciliationQueueDto { EscalatingConflicts = 2 } },
            new OperationalInventoryWorkbenchDto
            {
                DriftSummary = new OperationalInventoryDriftSummaryDto
                {
                    DriftSeverity = OperationalInventoryDriftSeverity.High,
                    EscalatingDriftConflicts = 1
                }
            },
            new OperationalGovernanceRuntimeProtectionSnapshot(),
            new OperationalGovernanceFingerprintSnapshot { FingerprintStability = "Volatile", FingerprintChanged = true },
            runtimeSaturationIndicated: true,
            protectiveModeActive: true,
            priorSnapshots: Array.Empty<OperationalIncidentCaseSnapshot>(),
            generatedAtUtc);

        Assert.True(cases.CaseCount <= OperationalIncidentAggregation.MaxIncidentCases);

        var repeat = OperationalIncidentAggregation.ComposeCases(
            new OperationalTrendSummaryDto { OverallDirection = OperationalTrendDirection.Degrading },
            new OperationalTimelineDto { EventCount = 1, Summary = "Timeline activity" },
            Array.Empty<OperationalTimelineCorrelationDto>(),
            new OperationalTriageQueueDto { OverallPriority = OperationalTriagePriority.High },
            new OperationalRecoveryPostureDto { OverallState = OperationalRecoveryState.Volatile },
            new OperationalDashboardSummaryDto(),
            replayPressure,
            new OperationalReplayStabilizationDto { ReplayPressureEscalating = true },
            new OperationalReplayRecoveryConfidenceDto { Confidence = OperationalReplayRecoveryConfidence.Fragile },
            new OperationalReconciliationWorkbenchDto { Queue = new OperationalReconciliationQueueDto { EscalatingConflicts = 2 } },
            new OperationalInventoryWorkbenchDto
            {
                DriftSummary = new OperationalInventoryDriftSummaryDto
                {
                    DriftSeverity = OperationalInventoryDriftSeverity.High,
                    EscalatingDriftConflicts = 1
                }
            },
            new OperationalGovernanceRuntimeProtectionSnapshot(),
            new OperationalGovernanceFingerprintSnapshot { FingerprintStability = "Volatile", FingerprintChanged = true },
            runtimeSaturationIndicated: true,
            protectiveModeActive: true,
            priorSnapshots: Array.Empty<OperationalIncidentCaseSnapshot>(),
            generatedAtUtc);

        Assert.Equal(
            cases.Cases.Select(c => c.IncidentId),
            repeat.Cases.Select(c => c.IncidentId));
    }

    [Fact]
    public void Incident_recurrence_detection_is_deterministic()
    {
        var prior = new[]
        {
            new OperationalIncidentCaseSnapshot
            {
                IncidentId = OperationalIncidentAggregation.IncidentReplayInstability,
                CategoryKey = "replay",
                RecommendedRoute = OperationalIncidentAggregation.RouteReplayWorkbench,
                ObservedAtUtc = DateTime.UtcNow.AddMinutes(-5)
            }
        };

        Assert.True(OperationalIncidentAggregation.DetectRecurrence(
            OperationalIncidentAggregation.IncidentReplayInstability,
            OperationalIncidentAggregation.RouteReplayWorkbench,
            "replay",
            prior));

        Assert.False(OperationalIncidentAggregation.DetectRecurrence(
            OperationalIncidentAggregation.IncidentInventoryDrift,
            OperationalIncidentAggregation.RouteInventoryWorkbench,
            "inventory",
            prior));
    }

    [Fact]
    public void Incident_routes_use_existing_operational_paths_only()
    {
        var cases = OperationalIncidentAggregation.ComposeCases(
            new OperationalTrendSummaryDto(),
            new OperationalTimelineDto(),
            Array.Empty<OperationalTimelineCorrelationDto>(),
            new OperationalTriageQueueDto(),
            new OperationalRecoveryPostureDto(),
            new OperationalDashboardSummaryDto(),
            new OperationalReplayPressureSummaryDto { InstabilityLevel = OperationalReplayPressureLevel.High },
            new OperationalReplayStabilizationDto(),
            new OperationalReplayRecoveryConfidenceDto(),
            new OperationalReconciliationWorkbenchDto(),
            new OperationalInventoryWorkbenchDto(),
            new OperationalGovernanceRuntimeProtectionSnapshot(),
            new OperationalGovernanceFingerprintSnapshot(),
            runtimeSaturationIndicated: false,
            protectiveModeActive: false,
            priorSnapshots: Array.Empty<OperationalIncidentCaseSnapshot>(),
            DateTime.UtcNow);

        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            OperationalIncidentAggregation.RouteDashboard,
            OperationalIncidentAggregation.RouteReconciliationWorkbench,
            OperationalIncidentAggregation.RouteInventoryWorkbench,
            OperationalIncidentAggregation.RouteReplayWorkbench,
            OperationalIncidentAggregation.RouteTrendSummary,
            OperationalIncidentAggregation.RouteTimeline,
            OperationalIncidentAggregation.RouteTriage,
            OperationalIncidentAggregation.RouteRecovery,
            OperationalIncidentAggregation.RouteNavigation
        };

        Assert.All(cases.Cases, c => Assert.Contains(c.RecommendedRoute, allowed));
    }

    [Fact]
    public void Incident_summary_uses_operator_wording_only()
    {
        var summary = OperationalIncidentAggregation.ComposeSummary(
            new[]
            {
                new OperationalIncidentCaseDto
                {
                    IncidentId = OperationalIncidentAggregation.IncidentReplayInstability,
                    Severity = OperationalIncidentSeverity.High,
                    State = OperationalIncidentState.Active,
                    IsEscalating = true
                }
            },
            new OperationalRecoveryPostureDto { Summary = "Operational recovery confidence improving" },
            DateTime.UtcNow);

        var forbidden = new[] { "Pipeline", "Governance", "Explainability", "Classifier", "Projection" };
        foreach (var term in forbidden)
        {
            Assert.DoesNotContain(term, summary.Summary, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(term, summary.PlatformStabilityState, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Incident_service_is_registered_in_program()
    {
        var program = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Program.cs"));
        Assert.Contains("IOperationalIncidentService", program, StringComparison.Ordinal);
        Assert.Contains("OperationalIncidentService", program, StringComparison.Ordinal);
        Assert.Contains("IOperationalIncidentCaseStore", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Incident_observability_anchor_exists()
    {
        var service = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalIncidentService.cs"));
        Assert.Contains("Operational incident observability:", service, StringComparison.Ordinal);
    }
}
