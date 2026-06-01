using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalIncidentCacheGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Incident_service_uses_cached_groups_path()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalIncidentCorrelationService.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("IOperationalDiagnosticsCache", text, StringComparison.Ordinal);
        Assert.Contains("GetIncidentGroupsCachedAsync", text, StringComparison.Ordinal);
        Assert.Contains("OperationalDiagnosticsCacheConstants.IncidentGroupsCacheKey", text, StringComparison.Ordinal);
        Assert.Contains("OperationalDiagnosticsCacheCategories.IncidentGroups", text, StringComparison.Ordinal);
        Assert.Contains("OperationalCacheAdaptiveTtlHelper.ResolveEffectiveTtl", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Incident_endpoints_reuse_cached_groups_not_repeated_signal_loads()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalIncidentCorrelationService.cs");
        var text = File.ReadAllText(path);
        var cachedCalls = CountOccurrences(text, "GetIncidentGroupsCachedAsync");
        Assert.True(cachedCalls >= 4, "Summary/high-risk/cascading/filtered paths should reuse cached groups.");
        Assert.DoesNotContain("LoadSignalsAsync(cancellationToken);\n        var filtered = signals", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Incident_cache_does_not_store_forensic_exports_or_raw_audit_entities()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalIncidentCorrelationService.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("List<CorrelatedIncidentItemDto>", text, StringComparison.Ordinal);
        Assert.DoesNotContain("OperationalForensicSnapshotDto", text, StringComparison.Ordinal);
        Assert.DoesNotContain("GetOrCreateAsync<OperationalAuditRecord>", text, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChangesAsync", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Task.WhenAll", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Parallel.", text, StringComparison.Ordinal);
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
