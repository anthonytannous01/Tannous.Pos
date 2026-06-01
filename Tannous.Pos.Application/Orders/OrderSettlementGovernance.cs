using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Application.Orders;

/// <summary>
/// Settlement math for finalize (amount tendered vs net captured vs change due). Internal order fields only — wire DTOs unchanged.
/// </summary>
public static class OrderSettlementGovernance
{
    public static void ApplySettlement(Order order, decimal amountTendered, decimal totalAmountOwed)
    {
        order.AmountTendered = amountTendered;
        order.ChangeDue = Math.Max(amountTendered - totalAmountOwed, 0m);
        order.NetCapturedAmount = amountTendered - order.ChangeDue;
    }

    /// <summary>
    /// Refund uses net captured revenue; falls back for orders finalized before settlement columns existed.
    /// </summary>
    public static decimal ResolveNetCapturedAmountForRefund(Order order)
    {
        if (order.NetCapturedAmount > 0)
            return order.NetCapturedAmount;

        var amountTendered = order.AmountTendered > 0
            ? order.AmountTendered
            : order.Payments.Sum(p => p.Amount);

        var changeDue = order.ChangeDue > 0
            ? order.ChangeDue
            : Math.Max(amountTendered - order.TotalAmount, 0m);

        return amountTendered - changeDue;
    }
}
