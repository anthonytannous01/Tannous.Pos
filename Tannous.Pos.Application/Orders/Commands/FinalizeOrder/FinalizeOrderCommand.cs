using MediatR;
using Tannous.Pos.Application.DTOs.Orders;

namespace Tannous.Pos.Application.Orders.Commands.FinalizeOrder;

public class FinalizeOrderCommand : IRequest<OrderDto>
{
    public Guid OrderId { get; set; }
    public List<PaymentDto> Payments { get; set; } = new();
    public string IdempotencyKey { get; set; } = string.Empty;

    /// <summary>Physical currency the cashier hands change back in ("USD" or "LBP").
    /// Cashier chooses per sale; defaults to USD. Drives per-currency drawer math.</summary>
    public string ChangeCurrency { get; set; } = "USD";

    /// <summary>
    /// Settle an order whose payments were already recorded, rather than supplying them now.
    /// The split-bill flow records each person's payment as it is collected, so by the time the
    /// order is fully paid there is nothing left to send and <see cref="Payments"/> is empty.
    /// The handler still sums existing payments and rejects underpayment, so this relaxes the
    /// "send at least one payment" request rule without weakening settlement.
    /// </summary>
    public bool SettleRecordedPayments { get; set; }
}

public class PaymentDto
{
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? TransactionId { get; set; }
    public string? Notes { get; set; }
    /// <summary>
    /// Currency in which this payment was tendered ("USD" or "LBP").
    /// Defaults to "USD" when not specified.
    /// </summary>
    public string TenderedCurrency { get; set; } = "USD";
}
