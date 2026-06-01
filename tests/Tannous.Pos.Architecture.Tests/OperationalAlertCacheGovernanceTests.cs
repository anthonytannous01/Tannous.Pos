using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalAlertCacheGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Alert_service_composes_from_cached_upstream_summaries()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalAlertSignalService.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("LoadCachedUpstreamDiagnosticsAsync", text, StringComparison.Ordinal);
        Assert.Contains("_resilience.GetSummaryAsync", text, StringComparison.Ordinal);
        Assert.Contains("_reconciliation.GetSummaryAsync", text, StringComparison.Ordinal);
        Assert.Contains("_incidents.GetSummaryAsync", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalDiagnosticsCache", text, StringComparison.Ordinal);
        Assert.Contains("GetSignalsCachedAsync", text, StringComparison.Ordinal);
        Assert.DoesNotContain("GetOrCreateAsync<OperationalForensicSnapshotDto>", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Alert_service_avoids_extra_ef_heavy_incident_and_pressure_paths()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalAlertSignalService.cs");
        var text = File.ReadAllText(path);
        Assert.DoesNotContain("GetPressureIndicatorsAsync", text, StringComparison.Ordinal);
        Assert.DoesNotContain("GetCascadingDegradationAsync", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Task.WhenAll", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Parallel.", text, StringComparison.Ordinal);
    }
}
