namespace Tannous.Pos.Application.Orders;

/// <summary>
/// Centralizes legacy order line tax math shared by create and finalize paths.
/// This is governance/drift prevention only: semantics stay a fixed 10% on subtotal before tax.
/// GOVERNANCE / RISK: receipt printing uses BusinessSettings.TaxRate — not unified with this path (see OrderFinancialTaxGovernance).
/// The public API uses
/// <see cref="MidpointRounding.AwayFromZero"/> at high precision so typical monetary inputs match the historical multiply-only path.
/// </summary>
public static class OrderFinancialGovernance
{
    /// <summary>Fixed decimal rate applied to subtotal (10%). Do not change without coordinated product + mobile change.</summary>
    public const decimal LegacyOrderFlowTaxRate = 0.1m;

    /// <summary>Tax amount for the legacy create/finalize order paths (identical to prior inline <c>subTotal * 0.1m</c>).</summary>
    public static decimal ComputeLegacyTaxOnSubtotal(decimal subTotalBeforeTax) =>
        decimal.Round(
            subTotalBeforeTax * LegacyOrderFlowTaxRate,
            28,
            MidpointRounding.AwayFromZero);
}
