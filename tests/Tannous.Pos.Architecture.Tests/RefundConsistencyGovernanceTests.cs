using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Governance anchors for internal refund consistency on paid void and financial observability.
/// </summary>
public class RefundConsistencyGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void PaymentRefund_entity_and_migration_exist()
    {
        var entityPath = Path.Combine(RepoRoot(), "Tannous.Pos.Domain", "Entities", "PaymentRefund.cs");
        var migrationPath = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Migrations", "20260516120000_AddPaymentRefunds.cs");
        Assert.True(File.Exists(entityPath));
        Assert.True(File.Exists(migrationPath));
        var entityText = File.ReadAllText(entityPath);
        var migrationText = File.ReadAllText(migrationPath);
        Assert.Contains("CorrelationId", entityText, StringComparison.Ordinal);
        Assert.Contains("OriginalPaymentId", entityText, StringComparison.Ordinal);
        Assert.Contains("PaymentRefunds", migrationText, StringComparison.Ordinal);
        Assert.Contains("IX_PaymentRefunds_OrderId", migrationText, StringComparison.Ordinal);
    }

    [Fact]
    public void VoidOrderCommandHandler_paid_void_refund_and_inventory_share_transaction()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "Commands", "VoidOrder", "VoidOrderCommandHandler.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("BeginTransactionAsync", text, StringComparison.Ordinal);
        Assert.Contains("PersistPaidVoidRefundsAsync", text, StringComparison.Ordinal);
        Assert.Contains("ReverseFinalizeInventoryDeductionsAsync", text, StringComparison.Ordinal);
        Assert.Contains("PaymentRefund", text, StringComparison.Ordinal);
        Assert.True(
            text.IndexOf("PersistPaidVoidRefundsAsync", StringComparison.Ordinal) <
            text.IndexOf("ReverseFinalizeInventoryDeductionsAsync", StringComparison.Ordinal),
            "Refund persistence should precede inventory reversal in paid void flow.");
    }

    [Fact]
    public void VoidOrderCommandHandler_refund_observability_anchors_are_stable()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "Commands", "VoidOrder", "VoidOrderCommandHandler.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("Refund consistency observability: beginning refund persistence", text, StringComparison.Ordinal);
        Assert.Contains("Refund consistency observability: refund persisted", text, StringComparison.Ordinal);
        Assert.Contains("Refund consistency observability: refund already exists", text, StringComparison.Ordinal);
        Assert.Contains("Refund consistency observability: concurrency conflict during refund", text, StringComparison.Ordinal);
        Assert.Contains("Refund consistency observability: paid void refund reconciliation", text, StringComparison.Ordinal);
        Assert.Contains("RefundAmount={RefundAmount}", text, StringComparison.Ordinal);
    }

    [Fact]
    public void VoidOrderCommandHandler_refund_idempotency_protection()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "Commands", "VoidOrder", "VoidOrderCommandHandler.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("refund already exists", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("r.OrderId == order.Id", text, StringComparison.Ordinal);
        Assert.Contains("ResolveNetCapturedAmountForRefund", text, StringComparison.Ordinal);
    }

    [Fact]
    public void FinalizeOrderCommandHandler_overpayment_observability_anchor()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "Commands", "FinalizeOrder", "FinalizeOrderCommandHandler.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("Financial consistency observability: overpayment detected", text, StringComparison.Ordinal);
        Assert.Contains("PaidAmount={PaidAmount}", text, StringComparison.Ordinal);
        Assert.Contains("ExpectedAmount={ExpectedAmount}", text, StringComparison.Ordinal);
        Assert.Contains("Difference={Difference}", text, StringComparison.Ordinal);
    }

    [Fact]
    public void VoidOrderCommandHandler_financial_consistency_refund_anchors()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "Commands", "VoidOrder", "VoidOrderCommandHandler.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("Financial consistency observability: refund persisted", text, StringComparison.Ordinal);
        Assert.Contains("Financial consistency observability: refund already exists", text, StringComparison.Ordinal);
    }
}
