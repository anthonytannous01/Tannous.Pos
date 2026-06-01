using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Governance anchors for internal operational audit timeline reconstruction.
/// </summary>
public class OperationalTimelineGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Operational_audit_timeline_service_exists_and_is_registered()
    {
        var iface = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", "IOperationalAuditTimelineService.cs");
        var impl = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalAuditTimelineService.cs");
        var program = Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Program.cs");

        Assert.True(File.Exists(iface), "Missing IOperationalAuditTimelineService");
        Assert.True(File.Exists(impl), "Missing OperationalAuditTimelineService");
        var programText = File.ReadAllText(program);
        Assert.Contains("IOperationalAuditTimelineService", programText, StringComparison.Ordinal);
        Assert.Contains("OperationalAuditTimelineService", programText, StringComparison.Ordinal);
    }

    [Fact]
    public void Timeline_service_supports_order_device_operation_and_entity_queries()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalAuditTimelineService.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("GetByOrderIdAsync", text, StringComparison.Ordinal);
        Assert.Contains("GetByDeviceIdAsync", text, StringComparison.Ordinal);
        Assert.Contains("GetByOperationIdAsync", text, StringComparison.Ordinal);
        Assert.Contains("GetByEntityAsync", text, StringComparison.Ordinal);
        Assert.Contains("OrderBy(r => r.CreatedAtUtc)", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Order_lifecycle_operational_audit_anchors_exist_in_finalize_handler()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "Commands", "FinalizeOrder", "FinalizeOrderCommandHandler.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("OperationalAuditActions.FinalizeSuccess", text, StringComparison.Ordinal);
        Assert.Contains("OperationalAuditActions.FinalizeReplayShortCircuit", text, StringComparison.Ordinal);
        Assert.Contains("OperationalAuditActions.LifecycleStateConflict", text, StringComparison.Ordinal);
        Assert.Contains("OperationalAuditActions.StaleOfflineMutation", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Settlement_and_refund_operational_audit_anchors_exist()
    {
        var finalize = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "Commands", "FinalizeOrder", "FinalizeOrderCommandHandler.cs"));
        var voidHandler = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "Commands", "VoidOrder", "VoidOrderCommandHandler.cs"));

        Assert.Contains("OperationalAuditCategories.Settlement", finalize, StringComparison.Ordinal);
        Assert.Contains("OperationalAuditActions.SettlementUnderpaymentRejected", finalize, StringComparison.Ordinal);
        Assert.Contains("OperationalAuditActions.SettlementOverpayment", finalize, StringComparison.Ordinal);

        Assert.Contains("OperationalAuditActions.RefundPersisted", voidHandler, StringComparison.Ordinal);
        Assert.Contains("OperationalAuditCategories.Refund", voidHandler, StringComparison.Ordinal);
    }

    [Fact]
    public void Inventory_reversal_operational_audit_anchors_exist()
    {
        var finalize = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "Commands", "FinalizeOrder", "FinalizeOrderCommandHandler.cs"));
        var voidHandler = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "Commands", "VoidOrder", "VoidOrderCommandHandler.cs"));

        Assert.Contains("OperationalAuditActions.InventoryDeductionPass", finalize, StringComparison.Ordinal);
        Assert.Contains("OperationalAuditActions.NegativeStockDetected", finalize, StringComparison.Ordinal);
        Assert.Contains("OperationalAuditActions.ReversalMovementPersisted", voidHandler, StringComparison.Ordinal);
        Assert.Contains("OperationalAuditCategories.Inventory", voidHandler, StringComparison.Ordinal);
    }
}
