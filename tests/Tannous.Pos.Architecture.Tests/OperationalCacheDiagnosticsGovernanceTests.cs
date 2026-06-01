using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalCacheDiagnosticsGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    private static string ControllerSource() =>
        File.ReadAllText(Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditCacheDiagnosticsController.cs"));

    private static string DiagnosticsServiceSource() =>
        File.ReadAllText(Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalDiagnosticsCacheDiagnosticsService.cs"));

    private static string CacheServiceSource() =>
        File.ReadAllText(Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalDiagnosticsCacheService.cs"));

    [Fact]
    public void Cache_diagnostics_routes_under_internal_operational_audit_cache()
    {
        var text = ControllerSource();
        Assert.Contains("internal/operational-audit/cache", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"summary\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"effectiveness\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"stale-risk\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"pressure\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"adaptive-summary\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"warm-candidates\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"stability\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"governance-overview\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"governance-audit\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"governance-consistency\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"survivability\")]", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Cache_diagnostics_endpoints_are_get_only_and_admin_only()
    {
        var text = ControllerSource();
        Assert.Contains("[Authorize(Policy = \"Admin\")]", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPut(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPatch(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpDelete(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[AllowAnonymous]", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Cache_diagnostics_do_not_expose_envelope_values_or_distributed_cache()
    {
        var controllerText = ControllerSource();
        var serviceText = DiagnosticsServiceSource();
        var cacheText = CacheServiceSource();

        Assert.DoesNotContain(".Value", controllerText, StringComparison.Ordinal);
        Assert.DoesNotContain("OperationalDiagnosticsCacheEnvelope<", controllerText, StringComparison.Ordinal);
        Assert.DoesNotContain("IDistributedCache", controllerText + serviceText + cacheText, StringComparison.Ordinal);
        Assert.DoesNotContain("Redis", controllerText + serviceText + cacheText, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChangesAsync", serviceText, StringComparison.Ordinal);
    }

    [Fact]
    public void Cache_diagnostics_service_uses_metadata_and_telemetry_only()
    {
        var text = OperationalDiagnosticsGovernanceTestSources.DiagnosticsAndProjectionsSource();
        Assert.Contains("GetDiagnosticsEntryMetadata", text, StringComparison.Ordinal);
        Assert.Contains("Operational cache diagnostics:", text, StringComparison.Ordinal);
        Assert.Contains("Operational cache effectiveness:", text, StringComparison.Ordinal);
        Assert.Contains("Operational cache stale visibility:", text, StringComparison.Ordinal);
        Assert.Contains("Operational cache pressure visibility:", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Task.WhenAll", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Parallel.", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Cache_service_exposes_diagnostics_metadata_projection()
    {
        var text = CacheServiceSource();
        Assert.Contains("GetDiagnosticsEntryMetadata", text, StringComparison.Ordinal);
        Assert.Contains("CacheKeyAlias", text, StringComparison.Ordinal);
        Assert.Contains("AgeSeconds", text, StringComparison.Ordinal);
        Assert.Contains("RemainingTtlSeconds", text, StringComparison.Ordinal);
    }
}
