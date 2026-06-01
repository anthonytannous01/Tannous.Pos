using System.Text.RegularExpressions;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Audit.Governance;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalGovernanceSnapshotReuseTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { RepoRoot() }.Concat(parts).ToArray()));

    [Fact]
    public void Snapshot_endpoints_are_get_only()
    {
        var controller = Read(
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditCacheDiagnosticsController.cs");

        Assert.Contains("[HttpGet(\"governance-snapshot\")]", controller, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"projection-reuse\")]", controller, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"projection-consistency\")]", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void Snapshot_keys_are_deterministic_and_low_cardinality()
    {
        Assert.Equal(3, OperationalGovernanceSnapshotKeys.All.Count);
        Assert.Equal(3, OperationalGovernanceSnapshotReuseConstants.MaxSnapshotKeys);
        Assert.Contains(OperationalGovernanceSnapshotKeys.Standard, OperationalGovernanceSnapshotKeys.All);
        Assert.Equal(
            OperationalGovernanceSnapshotKeys.Standard,
            OperationalGovernanceSnapshotKeys.ForProfile(OperationalGovernanceProfile.Standard));
    }

    [Fact]
    public void Snapshot_ttl_is_bounded()
    {
        Assert.InRange(
            OperationalGovernanceSnapshotReuseConstants.TtlSeconds,
            5,
            10);
        Assert.True(
            OperationalGovernanceSnapshotReuseConstants.AgingThresholdSeconds
            < OperationalGovernanceSnapshotReuseConstants.TtlSeconds);
    }

    [Fact]
    public void Snapshot_governance_types_avoid_business_payload_and_ef_entities()
    {
        var governanceDir = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", "Governance");
        var snapshotFiles = Directory.EnumerateFiles(governanceDir, "OperationalGovernanceSnapshot*.cs")
            .Concat(Directory.EnumerateFiles(governanceDir, "OperationalGovernanceProjectionReuse*.cs"))
            .Concat(Directory.EnumerateFiles(governanceDir, "OperationalGovernanceProjectionConsistency*.cs"))
            .ToList();

        Assert.NotEmpty(snapshotFiles);

        foreach (var file in snapshotFiles)
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("PosDbContext", text, StringComparison.Ordinal);
            Assert.DoesNotContain("DbSet<", text, StringComparison.Ordinal);
            Assert.DoesNotContain("SyncConflictRecord", text, StringComparison.Ordinal);
            Assert.DoesNotContain("SyncOperationReceipt", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Snapshot_store_uses_lazy_expiry_without_timers()
    {
        var store = Read(
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalDiagnosticsProjections",
            "OperationalGovernanceSnapshotStore.cs");

        Assert.Contains("OperationalGovernanceSnapshotReuseConstants.TtlSeconds", store, StringComparison.Ordinal);
        Assert.DoesNotContain("IHostedService", store, StringComparison.Ordinal);
        Assert.DoesNotContain("Timer", store, StringComparison.Ordinal);
    }

    [Fact]
    public void Collaborator_fanout_remains_within_budget_after_snapshot_collaborator()
    {
        var projectionsDir = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalDiagnosticsProjections");
        var collaboratorCount = Directory.EnumerateFiles(projectionsDir, "*Collaborator*.cs").Count();

        Assert.True(collaboratorCount <= OperationalGovernanceComplexityMetrics.MaxCollaboratorFanout);
        Assert.True(collaboratorCount <= OperationalGovernanceRuntimeBudget.MaxProjectionCollaborators);
    }

    [Fact]
    public void Diagnostics_service_wires_snapshot_projection_collaborator()
    {
        var service = Read(
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalDiagnosticsCacheDiagnosticsService.cs");

        Assert.Contains("OperationalGovernanceSnapshotProjectionCollaborator", service, StringComparison.Ordinal);
        Assert.Contains("OperationalGovernanceSnapshotStore", service, StringComparison.Ordinal);
        Assert.Contains("GetGovernanceSnapshotAsync", service, StringComparison.Ordinal);
    }

    [Fact]
    public void Projection_context_factory_reuses_snapshot_store()
    {
        var factory = Read(
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalDiagnosticsProjections",
            "OperationalDiagnosticsCacheProjectionContextFactory.cs");

        Assert.Contains("OperationalGovernanceSnapshotStore", factory, StringComparison.Ordinal);
        Assert.Contains("OperationalGovernanceProjectionMemoizer", factory, StringComparison.Ordinal);
        Assert.Contains("AcquireSnapshot", factory, StringComparison.Ordinal);
    }

    [Fact]
    public void Cache_diagnostics_endpoint_count_remains_within_surface_budget()
    {
        var controller = Read(
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditCacheDiagnosticsController.cs");
        var endpointCount = Regex.Matches(controller, @"\[HttpGet\(""").Count;

        Assert.True(endpointCount <= OperationalGovernanceSurfaceBudget.MaxCacheDiagnosticsGetEndpoints);
    }

    [Fact]
    public void Pressure_reset_invalidates_governance_snapshots()
    {
        var coordinator = Read(
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalDiagnosticsPressureResetCoordinator.cs");

        Assert.Contains("InvalidateAll()", coordinator, StringComparison.Ordinal);
    }

    [Fact]
    public void Cache_service_invalidates_governance_snapshots_on_removal()
    {
        var cacheService = Read(
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalDiagnosticsCacheService.cs");

        Assert.Contains("OperationalGovernanceSnapshotStore", cacheService, StringComparison.Ordinal);
        Assert.Contains("_snapshotStore.InvalidateAll()", cacheService, StringComparison.Ordinal);
    }
}
