using Tannous.Pos.Application.Orders;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Anchors legacy 10% tax math used by create/finalize (must stay aligned with <see cref="OrderFinancialGovernance"/>).
/// </summary>
public class OrderFinancialGovernanceTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(100, 10)]
    [InlineData(99.99, 9.999)]
    public void ComputeLegacyTaxOnSubtotal_matches_fixed_ten_percent(decimal subtotal, decimal expectedTax)
    {
        var tax = OrderFinancialGovernance.ComputeLegacyTaxOnSubtotal(subtotal);
        Assert.Equal(expectedTax, tax);
    }

    [Fact]
    public void Legacy_rate_constant_is_unchanged()
    {
        Assert.Equal(0.1m, OrderFinancialGovernance.LegacyOrderFlowTaxRate);
    }
}
