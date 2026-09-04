namespace Tannous.Pos.Application.Orders;

/// <summary>
/// Documents the tax path now that order rows and receipts share one source.
/// Order create and finalize both call <c>OrderFinancialGovernance.ComputeTaxOnSubtotal</c>, which reads
/// <c>BusinessSettings.TaxEnabled</c> and <c>BusinessSettings.TaxRate</c>. Receipts display the stored
/// <c>Order.TaxAmount</c> and never recompute. The earlier split, where order rows used a fixed 10% while
/// receipts used the configured percentage, is closed.
/// GOVERNANCE: tax applies only when <c>BusinessSettings.TaxApplies</c> is true. Never test TaxRate alone —
/// a stored rate means nothing while the switch is off.
/// </summary>
public static class OrderFinancialTaxGovernance
{
    /// <summary>Order create/finalize money paths compute tax from settings.</summary>
    public const string OrderTaxPathAnchor =
        "OrderFinancialGovernance.ComputeTaxOnSubtotal";

    /// <summary>Retained as the first-boot fallback when BusinessSettings does not yet exist.</summary>
    public const string LegacyFallbackAnchor =
        "OrderFinancialGovernance.ComputeLegacyTaxOnSubtotal";

    /// <summary>Receipts display the stored order tax; the label reads the configured rate.</summary>
    public const string ReceiptTaxPathAnchor =
        "BusinessSettings.TaxApplies";

    /// <summary>Refunds on paid void record payment totals; they do not recompute tax lines.</summary>
    public const string RefundTaxAssumptionAnchor =
        "refund amount mirrors sum of captured payments (order tax row unchanged on void)";
}
