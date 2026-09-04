namespace Tannous.Pos.Domain.Enums;

/// <summary>
/// One place to ask what an <see cref="OrderStatus"/> means, so status gates cannot fork.
///
/// Background: <see cref="OrderStatus.Open"/> is never assigned by any code path. Orders are
/// created as <see cref="OrderStatus.Pending"/> and move to <see cref="OrderStatus.Paid"/>.
/// Handlers that tested for <c>Open</c> alone were therefore unreachable in practice: split bill
/// returned 400 for every order, and voiding an unfinalized order failed the same way. Open is
/// kept in the enum for stored historical rows and is accepted here alongside Pending.
/// </summary>
public static class OrderStatusRules
{
    /// <summary>
    /// True when an order exists but has not been settled: still editable, still payable,
    /// still splittable. Use this instead of comparing to a single status value.
    /// </summary>
    /// <remarks>
    /// FinalizeOrderCommandHandler keeps an equivalent inline check because its exact source text
    /// is pinned by MoneyPathGovernanceSourceTests as a money-path anchor. The two must agree.
    /// </remarks>
    public static bool IsUnsettled(this OrderStatus status) =>
        status is OrderStatus.Open or OrderStatus.Pending;
}
