using Tannous.Pos.Application.Audit;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalResilienceCacheGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Resilience_service_uses_cache_abstraction_and_metrics_key()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalResilienceDiagnosticsService.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("IOperationalDiagnosticsCache", text, StringComparison.Ordinal);
        Assert.Contains("GetMetricsSnapshotCachedAsync", text, StringComparison.Ordinal);
        Assert.Contains("OperationalDiagnosticsCacheConstants.ResilienceMetricsCacheKey", text, StringComparison.Ordinal);
        Assert.Contains("OperationalDiagnosticsCacheCategories.ResilienceMetrics", text, StringComparison.Ordinal);
        Assert.Contains("OperationalCacheAdaptiveTtlHelper.ResolveEffectiveTtl", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Resilience_service_does_not_cache_dtos_or_use_parallelism()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalResilienceDiagnosticsService.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("OperationalResilienceMetricsSnapshot", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Task.WhenAll", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Parallel.", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IDistributedCache", text, StringComparison.Ordinal);
        Assert.DoesNotContain("OperationalForensicSnapshotDto", text, StringComparison.Ordinal);
        Assert.DoesNotContain("GetOrCreateAsync<OperationalResilienceSummaryDto>", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Resilience_cache_pressure_escalation_anchor_exists()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalResilienceDiagnosticsService.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("Operational cache pressure escalation:", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Resilience_service_has_no_persistence_writes()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalResilienceDiagnosticsService.cs");
        var text = File.ReadAllText(path);
        Assert.DoesNotContain("SaveChangesAsync", text, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChanges(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Stale_risk_classifier_supports_aging_and_near_expiry()
    {
        var created = DateTime.UtcNow.AddSeconds(-80);
        var expires = created.AddSeconds(100);
        var agingAt = created.AddSeconds(55);
        var nearExpiryAt = created.AddSeconds(95);

        Assert.Equal(
            OperationalDiagnosticsCacheStaleRisk.Aging,
            OperationalDiagnosticsCacheStaleRiskClassifier.Classify(created, expires, agingAt));

        Assert.Equal(
            OperationalDiagnosticsCacheStaleRisk.NearExpiry,
            OperationalDiagnosticsCacheStaleRiskClassifier.Classify(created, expires, nearExpiryAt));
    }
}
