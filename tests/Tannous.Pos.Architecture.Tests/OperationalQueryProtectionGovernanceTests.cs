using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalQueryProtectionGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Query_protection_clamps_date_ranges_and_pagination()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", "OperationalQueryProtection.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("NormalizeDateRange", text, StringComparison.Ordinal);
        Assert.Contains("NormalizePageSize", text, StringComparison.Ordinal);
        Assert.Contains("DateRangeClamped", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Audit_query_service_uses_query_protection_anchors()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalAuditQueryService.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("Operational query protection:", text, StringComparison.Ordinal);
        Assert.Contains("OperationalQueryProtection", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Reconciliation_query_service_logs_conflict_aggregation_protection()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "SyncConflictReconciliationService.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("Operational query protection:", text, StringComparison.Ordinal);
        Assert.Contains("conflict aggregation exceeds safe limit", text, StringComparison.OrdinalIgnoreCase);
    }
}
