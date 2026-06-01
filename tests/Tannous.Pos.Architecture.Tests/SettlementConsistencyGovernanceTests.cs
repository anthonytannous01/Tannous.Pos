using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class SettlementConsistencyGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Order_entity_has_settlement_fields()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Domain", "Entities", "Order.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("AmountTendered", text, StringComparison.Ordinal);
        Assert.Contains("ChangeDue", text, StringComparison.Ordinal);
        Assert.Contains("NetCapturedAmount", text, StringComparison.Ordinal);
    }

    [Fact]
    public void AddOrderSettlementFields_migration_exists()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Migrations", "20260516140000_AddOrderSettlementFields.cs");
        Assert.True(File.Exists(path));
        var text = File.ReadAllText(path);
        Assert.Contains("AmountTendered", text, StringComparison.Ordinal);
        Assert.Contains("ChangeDue", text, StringComparison.Ordinal);
        Assert.Contains("NetCapturedAmount", text, StringComparison.Ordinal);
    }

    [Fact]
    public void FinalizeOrderCommandHandler_contains_settlement_observability_anchors()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "Commands", "FinalizeOrder", "FinalizeOrderCommandHandler.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("Settlement consistency observability: exact payment", text, StringComparison.Ordinal);
        Assert.Contains("Settlement consistency observability: overpayment with change due", text, StringComparison.Ordinal);
        Assert.Contains("Settlement consistency observability: underpayment rejected", text, StringComparison.Ordinal);
        Assert.Contains("Settlement consistency observability: settlement persisted", text, StringComparison.Ordinal);
        Assert.Contains("NetCapturedAmount={NetCapturedAmount}", text, StringComparison.Ordinal);
    }

    [Fact]
    public void VoidOrderCommandHandler_refund_uses_net_captured_amount()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "Commands", "VoidOrder", "VoidOrderCommandHandler.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("ResolveNetCapturedAmountForRefund", text, StringComparison.Ordinal);
        Assert.Contains("Settlement consistency observability: refund uses net captured amount", text, StringComparison.Ordinal);
        Assert.Contains("Settlement consistency observability: change due excluded from refund", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Amount = paidAmount", text, StringComparison.Ordinal);
    }

    [Fact]
    public void OrderSettlementGovernance_computes_change_due_and_net_captured()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "OrderSettlementGovernance.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("ApplySettlement", text, StringComparison.Ordinal);
        Assert.Contains("ResolveNetCapturedAmountForRefund", text, StringComparison.Ordinal);
        Assert.Contains("ChangeDue", text, StringComparison.Ordinal);
        Assert.Contains("NetCapturedAmount", text, StringComparison.Ordinal);
    }
}
