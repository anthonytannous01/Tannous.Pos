using Tannous.Pos.Application.Orders;
using Tannous.Pos.Domain.Entities;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Anchors order tax math in <see cref="OrderFinancialGovernance"/>.
///
/// Step 121 changed rounding from 28 decimal places to currency precision. The old behaviour
/// produced totals that could not be tendered: a 1.50 subtotal at 11% gave 1.665, the client
/// displayed 1.66, and paying 1.66 was rejected as underpayment. Every tax result must now be
/// representable in cents.
/// </summary>
public class OrderFinancialGovernanceTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(100, 10.00)]
    [InlineData(99.99, 10.00)]   // 9.999 rounds away from zero to a payable 10.00
    [InlineData(1.50, 0.15)]
    public void ComputeLegacyTaxOnSubtotal_applies_ten_percent_at_currency_precision(decimal subtotal, decimal expectedTax)
    {
        var tax = OrderFinancialGovernance.ComputeLegacyTaxOnSubtotal(subtotal);
        Assert.Equal(expectedTax, tax);
    }

    [Fact]
    public void Legacy_rate_constant_is_unchanged()
    {
        Assert.Equal(0.1m, OrderFinancialGovernance.LegacyOrderFlowTaxRate);
    }

    [Theory]
    [InlineData(1.50, 11, 0.17)]    // was 0.165 — a total no cashier could tender
    [InlineData(100, 11, 11.00)]
    [InlineData(9.99, 11, 1.10)]
    [InlineData(1.50, 0, 0)]        // rate zero means no tax
    public void ComputeTaxOnSubtotal_uses_configured_rate(decimal subtotal, decimal ratePercent, decimal expectedTax)
    {
        var settings = new BusinessSettings { TaxEnabled = true, TaxRate = ratePercent };
        Assert.Equal(expectedTax, OrderFinancialGovernance.ComputeTaxOnSubtotal(subtotal, settings));
    }

    [Fact]
    public void ComputeTaxOnSubtotal_returns_zero_when_tax_is_switched_off()
    {
        // The stored rate must not leak through the switch — this is the whole point of TaxApplies.
        var settings = new BusinessSettings { TaxEnabled = false, TaxRate = 11m };
        Assert.Equal(0m, OrderFinancialGovernance.ComputeTaxOnSubtotal(100m, settings));
    }

    [Fact]
    public void ComputeTaxOnSubtotal_falls_back_to_legacy_before_settings_exist()
    {
        Assert.Equal(
            OrderFinancialGovernance.ComputeLegacyTaxOnSubtotal(100m),
            OrderFinancialGovernance.ComputeTaxOnSubtotal(100m, null));
    }

    [Theory]
    [InlineData(1.50, 11)]
    [InlineData(0.99, 11)]
    [InlineData(12.345, 7.5)]
    [InlineData(1234.56, 11)]
    public void Tax_is_always_payable_in_cents(decimal subtotal, decimal ratePercent)
    {
        var settings = new BusinessSettings { TaxEnabled = true, TaxRate = ratePercent };
        var tax = OrderFinancialGovernance.ComputeTaxOnSubtotal(subtotal, settings);

        // An order total is only payable if every component rounds to cents.
        Assert.Equal(decimal.Round(tax, OrderFinancialGovernance.CurrencyDecimals), tax);
    }
}
