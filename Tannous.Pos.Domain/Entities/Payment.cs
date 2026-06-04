using Tannous.Pos.Domain.Common;
using Tannous.Pos.Domain.Common.ValueObjects;

namespace Tannous.Pos.Domain.Entities;

public class Payment : BaseEntity, IAggregateRoot
{
    public string PaymentMethod { get; set; } = string.Empty; // Cash, Card, Mobile, etc.
    public decimal Amount { get; set; } = 0;
    public string? TransactionId { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public DateTime PaymentDate { get; set; }
    public bool IsSuccessful { get; set; } = true;

    // ── Lebanese market: dual-currency tender ───────────────────────────────
    /// <summary>
    /// Currency in which this payment was tendered ("USD" or "LBP").
    /// Defaults to "USD" for backward compatibility with existing rows.
    /// </summary>
    public string TenderedCurrency { get; set; } = "USD";

    /// <summary>
    /// Snapshot of the LBP/USD exchange rate at the moment of payment.
    /// Null when TenderedCurrency is USD or when rate was not configured.
    /// </summary>
    public decimal? ExchangeRateUsed { get; set; }

    /// <summary>
    /// Amount expressed in USD for reporting purposes.
    /// When TenderedCurrency == "USD": same as Amount.
    /// When TenderedCurrency == "LBP": Amount / ExchangeRateUsed (if rate > 0), else 0.
    /// </summary>
    public decimal AmountInUsd { get; set; } = 0;

    // Foreign keys
    public Guid OrderId { get; set; }

    // Navigation properties
    public virtual Order Order { get; set; } = null!;
}
