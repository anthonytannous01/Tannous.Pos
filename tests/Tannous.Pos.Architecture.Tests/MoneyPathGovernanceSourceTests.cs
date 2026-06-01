using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Source anchors for finalize/void money paths and shared legacy tax governance (no runtime behavior change).
/// </summary>
public class MoneyPathGovernanceSourceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void FinalizeOrderCommandHandler_retains_money_path_governance_anchors()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "Commands", "FinalizeOrder", "FinalizeOrderCommandHandler.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("order.Status == OrderStatus.Paid", text, StringComparison.Ordinal);
        Assert.Contains("Order already finalized", text, StringComparison.Ordinal);
        Assert.Contains("Finalize governance", text, StringComparison.Ordinal);

        Assert.Contains("order.Status is not (OrderStatus.Open or OrderStatus.Pending)", text, StringComparison.Ordinal);
        Assert.Contains("not in a finalizable status", text, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("BeginTransactionAsync", text, StringComparison.Ordinal);
        Assert.Contains("CommitAsync", text, StringComparison.Ordinal);
        Assert.Contains("RollbackAsync", text, StringComparison.Ordinal);

        Assert.Contains("recomputed from lines differs from persisted SubTotal", text, StringComparison.Ordinal);

        Assert.Contains("OrderFinancialSnapshotGovernance", text, StringComparison.Ordinal);
        Assert.Contains("OrderFinancialGovernance.ComputeLegacyTaxOnSubtotal", text, StringComparison.Ordinal);

        Assert.Contains("DbUpdateConcurrencyException", text, StringComparison.Ordinal);
        Assert.Contains("Money-path concurrency visibility: optimistic concurrency conflict during finalize", text, StringComparison.Ordinal);
    }

    [Fact]
    public void VoidOrderCommandHandler_retains_paid_void_and_persist_anchors()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "Commands", "VoidOrder", "VoidOrderCommandHandler.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("order.Status == OrderStatus.Paid", text, StringComparison.Ordinal);
        Assert.Contains("ReverseFinalizeInventoryDeductionsAsync", text, StringComparison.Ordinal);
        Assert.Contains("GOVERNANCE / RISK: Paid void restores inventory from finalize Sale movements only", text, StringComparison.Ordinal);

        Assert.Contains("order.Status = OrderStatus.Void", text, StringComparison.Ordinal);
        Assert.Contains("SaveChangesAsync", text, StringComparison.Ordinal);
        Assert.Contains("DbUpdateConcurrencyException", text, StringComparison.Ordinal);
        Assert.Contains("Money-path concurrency visibility: optimistic concurrency conflict during void", text, StringComparison.Ordinal);
    }

    [Fact]
    public void OrderFinancialGovernance_retains_legacy_tax_rounding_anchor()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "OrderFinancialGovernance.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("MidpointRounding.AwayFromZero", text, StringComparison.Ordinal);
        Assert.Contains("LegacyOrderFlowTaxRate", text, StringComparison.Ordinal);
    }
}
