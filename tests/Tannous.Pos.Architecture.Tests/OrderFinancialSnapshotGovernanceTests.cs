using Tannous.Pos.Application.Orders;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OrderFinancialSnapshotGovernanceTests
{
    [Theory]
    [InlineData(10, 1, 11, true)]
    [InlineData(0, 0, 0, true)]
    [InlineData(100, 10, 90, false)]
    [InlineData(-1, 0, 0, false)]
    public void HasConsistentNonNegativeSnapshot_matches_expected(
        decimal sub,
        decimal tax,
        decimal total,
        bool expectedOk)
    {
        var ok = OrderFinancialSnapshotGovernance.HasConsistentNonNegativeSnapshot(sub, tax, total, out var diag);
        Assert.Equal(expectedOk, ok);
        if (expectedOk)
            Assert.Null(diag);
        else
            Assert.NotNull(diag);
    }
}
