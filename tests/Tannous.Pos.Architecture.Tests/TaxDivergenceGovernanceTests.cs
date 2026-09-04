using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Anchors the tax path. The order-row vs receipt divergence was closed in Step 119: order
/// create, finalize and kiosk all resolve tax from BusinessSettings through one helper.
/// </summary>
public class TaxDivergenceGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void OrderFinancialTaxGovernance_documents_split_paths()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "OrderFinancialTaxGovernance.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("OrderFinancialGovernance.ComputeLegacyTaxOnSubtotal", text, StringComparison.Ordinal);
        Assert.Contains("BusinessSettings.TaxRate", text, StringComparison.Ordinal);
        Assert.Contains("refund amount mirrors sum of captured payments", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GOVERNANCE", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OrderFinancialGovernance_documents_configuration_driven_tax()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "OrderFinancialGovernance.cs");
        var text = File.ReadAllText(path);

        // Tax is resolved from settings, gated by TaxApplies, with the fixed-rate helper kept
        // only for the first boot before BusinessSettings exists.
        Assert.Contains("ComputeTaxOnSubtotal", text, StringComparison.Ordinal);
        Assert.Contains("TaxApplies", text, StringComparison.Ordinal);
        Assert.Contains("first-boot fallback", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PrintingService_documents_receipt_tax_path()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "Printing", "PrintingService.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("GOVERNANCE / RISK", text, StringComparison.Ordinal);
        Assert.Contains("BusinessSettings.TaxRate", text, StringComparison.Ordinal);
        Assert.Contains("OrderFinancialGovernance", text, StringComparison.Ordinal);
    }

    [Fact]
    public void VoidOrderCommandHandler_documents_refund_tax_assumption()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "Commands", "VoidOrder", "VoidOrderCommandHandler.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("OrderFinancialTaxGovernance", text, StringComparison.Ordinal);
        Assert.Contains("tax row on order is not recomputed", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FinalizeOrderCommandHandler_uses_legacy_order_tax_path()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "Commands", "FinalizeOrder", "FinalizeOrderCommandHandler.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("OrderFinancialGovernance.ComputeLegacyTaxOnSubtotal", text, StringComparison.Ordinal);
    }
}
