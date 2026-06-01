using Tannous.Pos.Domain.Enums;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Lightweight regression anchors for money/inventory/order semantics (wire and domain constants).
/// </summary>
public class DomainIntegrityGovernanceTests
{
    [Theory]
    [InlineData(OrderStatus.Open, 1)]
    [InlineData(OrderStatus.Pending, 2)]
    [InlineData(OrderStatus.Confirmed, 3)]
    [InlineData(OrderStatus.InPreparation, 4)]
    [InlineData(OrderStatus.Ready, 5)]
    [InlineData(OrderStatus.Paid, 6)]
    [InlineData(OrderStatus.Completed, 7)]
    [InlineData(OrderStatus.Cancelled, 8)]
    [InlineData(OrderStatus.Void, 9)]
    public void OrderStatus_numeric_values_are_stable_for_clients(OrderStatus status, int expected) =>
        Assert.Equal(expected, (int)status);

    [Theory]
    [InlineData(InventoryMovementType.Purchase, 1)]
    [InlineData(InventoryMovementType.Sale, 2)]
    [InlineData(InventoryMovementType.Adjustment, 3)]
    [InlineData(InventoryMovementType.Wastage, 4)]
    [InlineData(InventoryMovementType.Transfer, 5)]
    [InlineData(InventoryMovementType.Return, 6)]
    public void InventoryMovementType_numeric_values_are_stable(InventoryMovementType t, int expected) =>
        Assert.Equal(expected, (int)t);
}
