using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Shifts;

/// <summary>
/// Single source of truth for drawer cash math. Used by CloseShift (persisted figures)
/// and GetCurrentShift (live figures for the active-shift screen) so the two can never drift.
/// Requires the shift to be loaded with Orders→Payments and CashDrawerEvents.
///
/// Dual-currency drawer (Lebanon): USD and LBP notes share one physical drawer but are
/// reconciled independently — amounts are never converted between currencies here.
/// Payment.Amount is in the TENDERED currency (raw LBP for LBP payments); change is
/// handed out in Order.ChangeCurrency for Order.ChangeAmountInCurrency.
/// </summary>
public static class ShiftCashCalculator
{
    private const string Usd = "USD";
    private const string Lbp = "LBP";

    /// <summary>Expected USD notes: opening float + USD cash tendered − USD change given − USD drops.</summary>
    public static decimal ComputeExpectedCash(Shift shift) =>
        shift.OpeningBalance + ComputeCashSales(shift) - ComputeCashDrops(shift);

    /// <summary>Expected LBP notes: opening float + LBP cash tendered − LBP change given − LBP drops.</summary>
    public static decimal ComputeExpectedCashLbp(Shift shift) =>
        shift.OpeningBalanceLbp + ComputeCashSalesLbp(shift) - ComputeCashDropsLbp(shift);

    /// <summary>Net USD cash retained from paid orders. Card/Other payments never enter the
    /// physical drawer and must be excluded, otherwise every card sale appears as a cash
    /// shortage in the variance. Change handed out in USD is subtracted here; change handed
    /// out in LBP is subtracted on the LBP side instead.</summary>
    public static decimal ComputeCashSales(Shift shift) =>
        shift.Orders
            .Where(o => o.Status == OrderStatus.Paid)
            .Sum(o =>
            {
                var usdCashTendered = o.Payments
                    .Where(IsUsdCash)
                    .Sum(p => p.Amount);

                var usdChangeGiven = IsLbpCurrency(o.ChangeCurrency)
                    ? 0m
                    : Math.Min(Math.Max(o.ChangeDue, 0m), Math.Max(usdCashTendered, 0m));

                return usdCashTendered - usdChangeGiven;
            });

    /// <summary>Net LBP cash retained from paid orders (raw LBP amounts).</summary>
    public static decimal ComputeCashSalesLbp(Shift shift) =>
        shift.Orders
            .Where(o => o.Status == OrderStatus.Paid)
            .Sum(o =>
            {
                var lbpCashTendered = o.Payments
                    .Where(IsLbpCash)
                    .Sum(p => p.Amount); // raw LBP

                var lbpChangeGiven = IsLbpCurrency(o.ChangeCurrency)
                    ? Math.Max(o.ChangeAmountInCurrency, 0m)
                    : 0m;

                return lbpCashTendered - lbpChangeGiven;
            });

    public static decimal ComputeCashDrops(Shift shift) =>
        shift.CashDrawerEvents
            .Where(e => e.EventType == "Drop" && !IsLbpCurrency(e.Currency))
            .Sum(e => e.Amount ?? 0);

    public static decimal ComputeCashDropsLbp(Shift shift) =>
        shift.CashDrawerEvents
            .Where(e => e.EventType == "Drop" && IsLbpCurrency(e.Currency))
            .Sum(e => e.Amount ?? 0);

    /// <summary>USD physical cash: method CASH with USD (or unspecified legacy) tender.</summary>
    private static bool IsUsdCash(Payment p) =>
        string.Equals(p.PaymentMethod, "CASH", StringComparison.OrdinalIgnoreCase) &&
        !IsLbpCurrency(p.TenderedCurrency);

    /// <summary>LBP physical cash: "LBP Cash" method (split flow) or CASH tendered in LBP.</summary>
    private static bool IsLbpCash(Payment p) =>
        (p.PaymentMethod?.Contains("LBP", StringComparison.OrdinalIgnoreCase) == true &&
         p.PaymentMethod.Contains("Cash", StringComparison.OrdinalIgnoreCase))
        ||
        (string.Equals(p.PaymentMethod, "CASH", StringComparison.OrdinalIgnoreCase) &&
         IsLbpCurrency(p.TenderedCurrency));

    private static bool IsLbpCurrency(string? currency) =>
        string.Equals(currency, Lbp, StringComparison.OrdinalIgnoreCase);
}
