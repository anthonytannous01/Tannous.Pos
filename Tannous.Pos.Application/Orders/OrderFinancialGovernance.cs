using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Application.Orders;

/// <summary>
/// Centralizes legacy order line tax math shared by create and finalize paths.
/// This is governance/drift prevention only: semantics stay a fixed 10% on subtotal before tax.
/// Order paths now compute tax from BusinessSettings via <see cref="ComputeTaxOnSubtotal"/>; the legacy
/// fixed-rate helper below is retained only as a first-boot fallback when settings do not yet exist.
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

    /// <summary>
    /// Tax for order create and finalize, driven by configuration.
    ///   - settings null (first boot)      → legacy fixed-rate fallback
    ///   - tax switched off, or rate zero  → no tax
    ///   - otherwise                       → configured percentage of subtotal
    /// Create and finalize must both call this, or an open order shows tax the receipt will not charge.
    /// </summary>
    public static decimal ComputeTaxOnSubtotal(decimal subTotalBeforeTax, BusinessSettings? settings)
    {
        if (settings is null)
        {
            return ComputeLegacyTaxOnSubtotal(subTotalBeforeTax);
        }

        if (!settings.TaxApplies)
        {
            return 0m;
        }

        return decimal.Round(
            subTotalBeforeTax * (settings.TaxRate / 100m),
            28,
            MidpointRounding.AwayFromZero);
    }
}
