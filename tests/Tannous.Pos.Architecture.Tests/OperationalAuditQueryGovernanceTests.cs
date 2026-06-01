using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalAuditQueryGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Operational_audit_query_service_exists_and_is_registered()
    {
        var iface = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", "IOperationalAuditQueryService.cs");
        var impl = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalAuditQueryService.cs");
        var program = Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Program.cs");

        Assert.True(File.Exists(iface));
        Assert.True(File.Exists(impl));
        var programText = File.ReadAllText(program);
        Assert.Contains("IOperationalAuditQueryService", programText, StringComparison.Ordinal);
        Assert.Contains("OperationalAuditQueryService", programText, StringComparison.Ordinal);
    }

    [Fact]
    public void Query_service_orders_timelines_by_timestamp()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalAuditQueryService.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("OrderBy(r => r.CreatedAtUtc)", text, StringComparison.Ordinal);
        Assert.Contains("OrderByDescending(r => r.CreatedAtUtc)", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Conflict_actions_include_replay_and_reconciliation_anchors()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", "OperationalAuditConflictActions.cs");
        var text = File.ReadAllText(path);
        foreach (var action in new[]
                 {
                     "ReplayMismatch",
                     "ConcurrencyConflict",
                     "LifecycleStateConflict",
                     "PartialBatchReconciliation",
                     "StaleOfflineMutation",
                     "MixedBatchOutcomes",
                     "NegativeStockDetected"
                 })
        {
            Assert.Contains(action, text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Metadata_projection_blocks_payload_and_stack_keys()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalAuditMetadataProjection.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("\"payload\"", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"stackTrace\"", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("MaxValueLength", text, StringComparison.Ordinal);
    }
}
