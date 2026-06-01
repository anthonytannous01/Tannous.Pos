using System.Text.RegularExpressions;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Audit.Governance;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalGovernanceFingerprintingTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { RepoRoot() }.Concat(parts).ToArray()));

    [Fact]
    public void Fingerprint_endpoints_are_get_only()
    {
        var controller = Read(
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditCacheDiagnosticsController.cs");

        Assert.Contains("[HttpGet(\"governance-fingerprint\")]", controller, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"governance-drift-analysis\")]", controller, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"replay-consistency\")]", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void Fingerprint_hash_is_deterministic_without_timestamp_inputs()
    {
        var builder = Read(
            "Tannous.Pos.Application",
            "Audit",
            "Governance",
            "OperationalGovernanceFingerprintBuilder.cs");

        Assert.Contains("SHA256.HashData", builder, StringComparison.Ordinal);

        var segmentsStart = builder.IndexOf(
            "internal static IReadOnlyList<string> BuildSegments",
            StringComparison.Ordinal);
        var segmentsEnd = builder.IndexOf(
            "public static class OperationalGovernanceFingerprintBuilder",
            StringComparison.Ordinal);
        Assert.True(segmentsStart >= 0 && segmentsEnd > segmentsStart);
        var segmentsSection = builder[segmentsStart..segmentsEnd];
        Assert.DoesNotContain("DateTime", segmentsSection, StringComparison.Ordinal);
    }

    [Fact]
    public void Signature_builder_excludes_payload_and_entity_references()
    {
        var signatureBuilder = Read(
            "Tannous.Pos.Application",
            "Audit",
            "Governance",
            "OperationalGovernanceFingerprintBuilder.cs");

        Assert.DoesNotContain("PosDbContext", signatureBuilder, StringComparison.Ordinal);
        Assert.DoesNotContain("SyncConflictRecord", signatureBuilder, StringComparison.Ordinal);
        Assert.DoesNotContain("SyncOperationReceipt", signatureBuilder, StringComparison.Ordinal);
        Assert.True(
            OperationalGovernanceFingerprintConstants.MaxSignatureSegments
            <= OperationalGovernanceRuntimeBudget.MaxExplainabilitySignals * 2);
    }

    [Fact]
    public void Fingerprint_governance_types_avoid_business_payload_and_ef_entities()
    {
        var governanceDir = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", "Governance");
        var fingerprintFiles = Directory.EnumerateFiles(governanceDir, "OperationalGovernanceFingerprint*.cs")
            .Concat(Directory.EnumerateFiles(governanceDir, "OperationalGovernanceDrift*.cs"))
            .Concat(Directory.EnumerateFiles(governanceDir, "OperationalGovernanceProjectionSignature*.cs"))
            .ToList();

        Assert.NotEmpty(fingerprintFiles);

        foreach (var file in fingerprintFiles)
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("PosDbContext", text, StringComparison.Ordinal);
            Assert.DoesNotContain("DbSet<", text, StringComparison.Ordinal);
            Assert.DoesNotContain("SyncConflictRecord", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Fingerprint_history_store_is_bounded_and_process_local()
    {
        var history = Read(
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalDiagnosticsProjections",
            "OperationalGovernanceFingerprintHistoryStore.cs");

        Assert.Contains("MaxPreviousFingerprintEntries", history, StringComparison.Ordinal);
        Assert.DoesNotContain("IHostedService", history, StringComparison.Ordinal);
        Assert.DoesNotContain("Redis", history, StringComparison.Ordinal);
    }

    [Fact]
    public void Collaborator_fanout_remains_within_budget_after_fingerprint_collaborator()
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
    public void Diagnostics_service_wires_fingerprint_projection_collaborator()
    {
        var service = Read(
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalDiagnosticsCacheDiagnosticsService.cs");

        Assert.Contains("OperationalGovernanceFingerprintProjectionCollaborator", service, StringComparison.Ordinal);
        Assert.Contains("GetGovernanceFingerprintAsync", service, StringComparison.Ordinal);
        Assert.Contains("GetGovernanceDriftAnalysisAsync", service, StringComparison.Ordinal);
        Assert.Contains("GetReplayConsistencyAsync", service, StringComparison.Ordinal);
    }

    [Fact]
    public void Snapshot_store_records_fingerprint_on_build()
    {
        var store = Read(
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalDiagnosticsProjections",
            "OperationalGovernanceSnapshotStore.cs");

        Assert.Contains("OperationalGovernanceFingerprintHistoryStore", store, StringComparison.Ordinal);
        Assert.Contains("RecordBuild", store, StringComparison.Ordinal);
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
    public void Fingerprint_hash_length_is_bounded()
    {
        Assert.Equal(16, OperationalGovernanceFingerprintConstants.FingerprintHashHexLength);
        Assert.Equal(1, OperationalGovernanceFingerprintConstants.MaxPreviousFingerprintEntries);
    }
}
