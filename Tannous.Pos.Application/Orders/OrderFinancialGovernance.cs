using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Application.Orders;

/// <summary>
/// Centralizes legacy order line tax math shared by create and finalize paths.
/// This is governance/drift prevention only: semantics stay a fixed 10% on subtotal before tax.
/// Order paths now compute tax from BusinessSettings via <see cref="ComputeTaxOnSubtotal"/>; the legacy
/// fixed-rate helper below is retained only as a first-boot fallback when settings do not yet exist.
/// Tax is rounded to <see cref="CurrencyDecimals"/> using <see cref="MidpointRounding.AwayFromZero"/>.
/// Rounding to currency precision is required, not cosmetic: at higher precision a 1.50 subtotal at
/// 11% produced a 1.665 total, which cannot be tendered in cents. Paying the displayed 1.66 was
/// rejected as underpayment, so exact payment was impossible and only overpaying with change worked.
/// </summary>
public static class OrderFinancialGovernance
{
    /// <summary>Fixed decimal rate applied to subtotal (10%). Do not change without coordinated product + mobile change.</summary>
    public const decimal LegacyOrderFlowTaxRate = 0.1m;

    /// <summary>Money must round to cents. An order total that cannot be tendered is not payable.</summary>
    public const int CurrencyDecimals = 2;

    /// <summary>
    /// Tax for the first-boot fallback, before BusinessSettings exists. Rounded to currency
    /// precision like every other money path; the earlier 28-digit rounding could yield a total
    /// that no cashier could tender.
    /// </summary>
    public static decimal ComputeLegacyTaxOnSubtotal(decimal subTotalBeforeTax) =>
        decimal.Round(
            subTotalBeforeTax * LegacyOrderFlowTaxRate,
            CurrencyDecimals,
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
            CurrencyDecimals,
            MidpointRounding.AwayFromZero);
    }
}
