using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Source anchors for paid void inventory reversal (movements, transaction boundary, observability, idempotency).
/// </summary>
public class PaidVoidInventoryReversalGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void VoidOrderCommandHandler_uses_transaction_boundary_for_reversal_and_void()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "Commands", "VoidOrder", "VoidOrderCommandHandler.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("BeginTransactionAsync", text, StringComparison.Ordinal);
        Assert.Contains("CommitAsync", text, StringComparison.Ordinal);
        Assert.Contains("RollbackAsync", text, StringComparison.Ordinal);
        Assert.Contains("IsolationLevel.Serializable", text, StringComparison.Ordinal);
    }

    [Fact]
    public void VoidOrderCommandHandler_persists_reversal_movements_from_finalize_sale_history()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "Commands", "VoidOrder", "VoidOrderCommandHandler.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("MovementType == InventoryMovementType.Sale", text, StringComparison.Ordinal);
        Assert.Contains("InventoryMovementType.Return", text, StringComparison.Ordinal);
        Assert.Contains("AddMovementAsync", text, StringComparison.Ordinal);
        Assert.Contains("ReversedMovementId", text, StringComparison.Ordinal);
    }

    [Fact]
    public void VoidOrderCommandHandler_reversal_observability_anchors_are_stable()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "Commands", "VoidOrder", "VoidOrderCommandHandler.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("Inventory reversal observability: beginning paid void reversal", text, StringComparison.Ordinal);
        Assert.Contains("Inventory reversal observability: reversal movements persisted", text, StringComparison.Ordinal);
        Assert.Contains("Inventory reversal observability: reversal already completed", text, StringComparison.Ordinal);
        Assert.Contains("Inventory reversal observability: stock restored after reversal", text, StringComparison.Ordinal);
        Assert.Contains("Inventory reversal observability: concurrency conflict during reversal", text, StringComparison.Ordinal);
        Assert.Contains("ReversalMovementCount", text, StringComparison.Ordinal);
        Assert.Contains("IdempotencyKey={IdempotencyKey}", text, StringComparison.Ordinal);
    }

    [Fact]
    public void VoidOrderCommandHandler_idempotent_reversal_short_circuit_before_double_restore()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "Commands", "VoidOrder", "VoidOrderCommandHandler.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("reversal already completed", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("-Void", text, StringComparison.Ordinal);
    }

    [Fact]
    public void InventoryMovement_entity_supports_reversed_movement_link()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Domain", "Entities", "InventoryMovement.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("ReversedMovementId", text, StringComparison.Ordinal);
    }

    [Fact]
    public void AddInventoryMovementReversedMovementId_migration_exists()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Migrations", "20260516100000_AddInventoryMovementReversedMovementId.cs");
        Assert.True(File.Exists(path));
        var text = File.ReadAllText(path);
        Assert.Contains("ReversedMovementId", text, StringComparison.Ordinal);
        Assert.Contains("InventoryMovements", text, StringComparison.Ordinal);
    }
}
