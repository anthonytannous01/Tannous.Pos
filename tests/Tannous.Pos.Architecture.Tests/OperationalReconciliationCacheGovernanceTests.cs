using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalReconciliationCacheGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Reconciliation_summary_uses_cache_abstraction()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "SyncConflictReconciliationService.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("IOperationalDiagnosticsCache", text, StringComparison.Ordinal);
        Assert.Contains("GetSummaryCachedAsync", text, StringComparison.Ordinal);
        Assert.Contains("OperationalDiagnosticsCacheConstants.ReconciliationSummaryCacheKey", text, StringComparison.Ordinal);
        Assert.Contains("OperationalDiagnosticsCacheCategories.ReconciliationSummary", text, StringComparison.Ordinal);
        Assert.Contains("OperationalCacheAdaptiveTtlHelper.ResolveEffectiveTtl", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Reconciliation_cache_does_not_store_raw_conflicts_or_parallel_ef()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "SyncConflictReconciliationService.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("ReconciliationSummaryDto", text, StringComparison.Ordinal);
        Assert.DoesNotContain("GetOrCreateAsync<SyncConflictRecord>", text, StringComparison.Ordinal);
        Assert.DoesNotContain("GetOrCreateAsync<List<SyncConflictRecord>>", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Task.WhenAll", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Parallel.", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IDistributedCache", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Reconciliation_cache_pressure_escalation_anchor_exists()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "SyncConflictReconciliationService.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("Operational cache pressure escalation:", text, StringComparison.Ordinal);
    }
}
