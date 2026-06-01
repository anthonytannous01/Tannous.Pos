using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalRetentionGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Retention_constants_define_hot_warm_and_forensic_windows()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", "OperationalRetentionConstants.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("HotOperationalWindowDays", text, StringComparison.Ordinal);
        Assert.Contains("WarmReconciliationWindowDays", text, StringComparison.Ordinal);
        Assert.Contains("LongTermForensicWindowDays", text, StringComparison.Ordinal);
        Assert.Contains("MaxQueryDateRangeDays", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Retention_governance_documents_non_goals()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", "OperationalRetentionGovernance.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("no automatic pruning", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no physical archival", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no S3", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Retention_summary_endpoint_exists_with_admin_authorization()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Controllers", "Internal", "OperationalAuditRetentionController.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("internal/operational-audit/retention", text, StringComparison.Ordinal);
        Assert.Contains("summary", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[Authorize(Policy = \"Admin\")]", text, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Conflict_lifecycle_classifier_defines_aging_severity()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", "OperationalConflictLifecycleClassifier.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("ClassifyAgingSeverity", text, StringComparison.Ordinal);
        Assert.Contains("GetEscalationRecommendation", text, StringComparison.Ordinal);
        Assert.Contains("NO background jobs", text, StringComparison.OrdinalIgnoreCase);
    }
}
