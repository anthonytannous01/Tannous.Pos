using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class SettlementFinancialIntegrityGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void FinalizeOrderCommandHandler_persists_change_due_and_net_captured_on_order()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "Commands", "FinalizeOrder", "FinalizeOrderCommandHandler.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("order.ChangeDue = changeDue", text, StringComparison.Ordinal);
        Assert.Contains("order.NetCapturedAmount = netCaptured", text, StringComparison.Ordinal);
        Assert.Contains("order.AmountTendered = totalPayments", text, StringComparison.Ordinal);
    }

    [Fact]
    public void FinalizeOrderCommandHandler_retains_overpayment_financial_consistency_log()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "Commands", "FinalizeOrder", "FinalizeOrderCommandHandler.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("Financial consistency observability: overpayment detected", text, StringComparison.Ordinal);
    }

    [Fact]
    public void VoidOrderCommandHandler_retains_refund_idempotency_anchors()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "Commands", "VoidOrder", "VoidOrderCommandHandler.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("Refund consistency observability: refund already exists", text, StringComparison.Ordinal);
        Assert.Contains("ReverseFinalizeInventoryDeductionsAsync", text, StringComparison.Ordinal);
    }

    [Fact]
    public void OrderSettlementGovernance_refund_resolver_excludes_change_from_tendered()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "OrderSettlementGovernance.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("amountTendered - changeDue", text, StringComparison.OrdinalIgnoreCase);
    }
}
