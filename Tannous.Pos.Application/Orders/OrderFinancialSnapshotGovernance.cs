using Microsoft.Extensions.Logging;

namespace Tannous.Pos.Application.Orders;

/// <summary>
/// Non-breaking financial snapshot checks for order finalize/create paths.
/// Violations log warnings only; they do not change totals or reject requests (governance visibility).
/// </summary>
public static class OrderFinancialSnapshotGovernance
{
    /// <summary>
    /// Returns false when the snapshot looks inconsistent or negative; <paramref name="diagnostic"/> is for logging only.
    /// </summary>
    public static bool HasConsistentNonNegativeSnapshot(
        decimal subTotal,
        decimal taxAmount,
        decimal totalAmount,
        out string? diagnostic)
    {
        if (subTotal < 0 || taxAmount < 0 || totalAmount < 0)
        {
            diagnostic =
                $"Negative component detected: subTotal={subTotal}, taxAmount={taxAmount}, totalAmount={totalAmount}";
            return false;
        }

        if (taxAmount > 0 && totalAmount + 0.0001m < subTotal)
        {
            diagnostic =
                $"When tax is positive, total should be at least subtotal; subTotal={subTotal}, taxAmount={taxAmount}, totalAmount={totalAmount}";
            return false;
        }

        diagnostic = null;
        return true;
    }

    public static void LogIfSnapshotViolatesInvariants(
        ILogger logger,
        Guid orderId,
        decimal subTotal,
        decimal taxAmount,
        decimal totalAmount)
    {
        if (HasConsistentNonNegativeSnapshot(subTotal, taxAmount, totalAmount, out var diagnostic))
            return;

        logger.LogWarning(
            "Order financial snapshot governance violation. OrderId={OrderId}, Detail={Detail}",
            orderId,
            diagnostic);
    }
}
