using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Shifts;

/// <summary>
/// Single source of truth for drawer cash math. Used by CloseShift (persisted figures)
/// and GetCurrentShift (live figures for the active-shift screen) so the two can never drift.
/// Requires the shift to be loaded with Orders→Payments and CashDrawerEvents.
/// </summary>
public static class ShiftCashCalculator
{
    /// <summary>Net cash retained in the drawer from paid orders in this shift.
    /// Card/Other payments never enter the physical drawer and must be excluded,
    /// otherwise every card sale appears as a cash shortage in the variance.
    /// Payment.Amount is the amount TENDERED (e.g. a $20 bill on a $16 order), and
    /// change is handed back out of the drawer, so the drawer impact is
    /// tendered − change due — not the raw tendered amount.</summary>
    public static decimal ComputeCashSales(Shift shift) =>
        shift.Orders
            .Where(o => o.Status == OrderStatus.Paid)
            .Sum(o =>
            {
                var cashTendered = o.Payments
                    .Where(p => string.Equals(p.PaymentMethod, "CASH", StringComparison.OrdinalIgnoreCase))
                    .Sum(p => p.Amount);
                if (cashTendered <= 0) return 0m;

                // Change is given from the drawer in cash. Cap at cashTendered as a guard:
                // change can never exceed the cash portion handed over.
                var changeGivenFromDrawer = Math.Min(Math.Max(o.ChangeDue, 0m), cashTendered);
                return cashTendered - changeGivenFromDrawer;
            });

    public static decimal ComputeCashDrops(Shift shift) =>
        shift.CashDrawerEvents
            .Where(e => e.EventType == "Drop")
            .Sum(e => e.Amount ?? 0);

    /// <summary>Expected cash = opening float + cash sales − cash drops removed from drawer.</summary>
    public static decimal ComputeExpectedCash(Shift shift) =>
        shift.OpeningBalance + ComputeCashSales(shift) - ComputeCashDrops(shift);
}
