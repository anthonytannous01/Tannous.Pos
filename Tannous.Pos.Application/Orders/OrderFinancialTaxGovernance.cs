namespace Tannous.Pos.Application.Orders;

/// <summary>
/// Documents the intentional split between order-row tax (legacy 10% on subtotal) and receipt printing tax
/// (receipt printing uses <c>BusinessSettings.TaxRate</c> — see PrintingService).
/// GOVERNANCE: do not unify without coordinated product, mobile, and reporting change.
/// </summary>
public static class OrderFinancialTaxGovernance
{
    /// <summary>Order create/finalize/void/refund money paths use <see cref="OrderFinancialGovernance.ComputeLegacyTaxOnSubtotal"/>.</summary>
    public const string OrderTaxPathAnchor =
        "OrderFinancialGovernance.ComputeLegacyTaxOnSubtotal";

    /// <summary>Printed receipts use settings percentage tax, not necessarily order row tax.</summary>
    public const string ReceiptTaxPathAnchor =
        "BusinessSettings.TaxRate";

    /// <summary>Refunds on paid void record payment totals; they do not recompute tax lines.</summary>
    public const string RefundTaxAssumptionAnchor =
        "refund amount mirrors sum of captured payments (order tax row unchanged on void)";
}
