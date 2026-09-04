using Tannous.Pos.Domain.Enums;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Guards order status gates.
///
/// OrderStatus.Open is never assigned: CreateOrderCommandHandler produces Pending. Handlers that
/// tested for Open alone were unreachable — split bill returned 400 for every order and voiding an
/// unfinalized order failed the same way. These tests keep the rule in one place.
/// </summary>
public class OrderStatusGateGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Theory]
    [InlineData(OrderStatus.Open, true)]
    [InlineData(OrderStatus.Pending, true)]
    [InlineData(OrderStatus.Paid, false)]
    [InlineData(OrderStatus.Void, false)]
    [InlineData(OrderStatus.Cancelled, false)]
    [InlineData(OrderStatus.Completed, false)]
    public void IsUnsettled_accepts_only_pre_settlement_statuses(OrderStatus status, bool expected)
    {
        Assert.Equal(expected, status.IsUnsettled());
    }

    [Fact]
    public void No_handler_gates_on_Open_alone()
    {
        // "!= OrderStatus.Open" without also accepting Pending is always-true in practice,
        // because nothing ever assigns Open. That is what broke split bill.
        var offenders = new List<string>();
        var applicationRoot = Path.Combine(RepoRoot(), "Tannous.Pos.Application");

        foreach (var file in Directory.EnumerateFiles(applicationRoot, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            var index = text.IndexOf("!= OrderStatus.Open", StringComparison.Ordinal);
            while (index >= 0)
            {
                // Accept it only when the same expression also admits Pending.
                var window = text.Substring(index, Math.Min(120, text.Length - index));
                if (!window.Contains("OrderStatus.Pending", StringComparison.Ordinal))
                {
                    offenders.Add(Path.GetRelativePath(RepoRoot(), file));
                    break;
                }
                index = text.IndexOf("!= OrderStatus.Open", index + 1, StringComparison.Ordinal);
            }
        }

        Assert.True(
            offenders.Count == 0,
            "Gate on OrderStatus.IsUnsettled() instead of comparing to Open alone. Offenders: "
                + string.Join(", ", offenders));
    }

    [Fact]
    public void Split_bill_handlers_use_the_shared_rule()
    {
        foreach (var rel in new[]
                 {
                     Path.Combine("Orders", "Queries", "GetSplitBill", "GetSplitBillQueryHandler.cs"),
                     Path.Combine("Orders", "Commands", "RecordSplitPayment", "RecordSplitPaymentCommandHandler.cs"),
                 })
        {
            var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", rel);
            var text = File.ReadAllText(path);
            Assert.Contains("IsUnsettled()", text, StringComparison.Ordinal);
        }
    }
}
