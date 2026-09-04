using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Anchors financial drift visibility (logging + shared helpers), not calculation semantics.
/// </summary>
public class OrderFinancialInvariantGovernanceSourceTests
{
    [Fact]
    public void FinalizeOrderCommandHandler_logs_subtotal_drift_warning_template()
    {
        var path = Path.Combine(ObservabilitySourceGovernanceTests.RepoRoot(), "Tannous.Pos.Application", "Orders", "Commands", "FinalizeOrder", "FinalizeOrderCommandHandler.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("Order subtotal recomputed from lines differs from persisted SubTotal", text, StringComparison.Ordinal);
        Assert.Contains("LogWarning", text, StringComparison.Ordinal);
    }

    [Fact]
    public void OrderFinancialSnapshotGovernance_invoked_from_create_and_finalize_handlers()
    {
        var root = ObservabilitySourceGovernanceTests.RepoRoot();
        foreach (var rel in new[] { "Commands\\CreateOrder\\CreateOrderCommandHandler.cs", "Commands\\FinalizeOrder\\FinalizeOrderCommandHandler.cs" })
        {
            var path = Path.Combine(root, "Tannous.Pos.Application", "Orders", rel);
            var text = File.ReadAllText(path);
            Assert.Contains("OrderFinancialSnapshotGovernance.LogIfSnapshotViolatesInvariants", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Order_tax_is_computed_only_through_OrderFinancialGovernance()
    {
        var root = ObservabilitySourceGovernanceTests.RepoRoot();

        // Every path that creates or settles an order, including kiosk. Before Step 119 the tax
        // rule existed in four places and two of them ignored BusinessSettings entirely, so an
        // open order showed tax the receipt would not charge.
        var handlers = new[]
        {
            Path.Combine(root, "Tannous.Pos.Application", "Orders", "Commands", "CreateOrder", "CreateOrderCommandHandler.cs"),
            Path.Combine(root, "Tannous.Pos.Application", "Orders", "Commands", "FinalizeOrder", "FinalizeOrderCommandHandler.cs"),
            Path.Combine(root, "Tannous.Pos.Application", "Kiosk", "Commands", "CreateKioskOrder", "CreateKioskOrderCommandHandler.cs"),
        };

        foreach (var path in handlers)
        {
            Assert.True(File.Exists(path), $"Missing {path}");
            var text = File.ReadAllText(path);

            Assert.Contains("OrderFinancialGovernance.ComputeTaxOnSubtotal", text, StringComparison.Ordinal);

            // An inline percentage calculation means a fifth copy of the rule has appeared.
            Assert.DoesNotContain("TaxRate / 100m", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void PrintingService_documents_settings_tax_vs_order_legacy_tax_split()
    {
        var path = Path.Combine(ObservabilitySourceGovernanceTests.RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "Printing", "PrintingService.cs");
        Assert.True(File.Exists(path), $"Missing {path}");
        var text = File.ReadAllText(path);
        Assert.Contains("BusinessSettings.TaxRate", text, StringComparison.Ordinal);
        Assert.Contains("legacy", text, StringComparison.OrdinalIgnoreCase);
    }
}
