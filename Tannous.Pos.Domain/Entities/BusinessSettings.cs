using Tannous.Pos.Domain.Common;
using Tannous.Pos.Domain.Common.ValueObjects;

namespace Tannous.Pos.Domain.Entities;

public class BusinessSettings : BaseEntity, IAggregateRoot
{
    public string BusinessName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? TaxNumber { get; set; }
    public decimal TaxRate { get; set; } = 0.0m;
    public string Currency { get; set; } = "USD";
    public string? ReceiptHeader { get; set; }
    public string? ReceiptFooter { get; set; }
    public bool RequireCustomerInfo { get; set; } = false;
    public bool EnableInventoryTracking { get; set; } = true;
    public bool EnableRecipeManagement { get; set; } = true;

    // ── Lebanese market: dual-currency support ──────────────────────────────
    /// <summary>
    /// LBP per 1 USD exchange rate. 0 means not configured (dual-currency display disabled).
    /// Example: 89500 means 1 USD = 89,500 LBP.
    /// </summary>
    public decimal ExchangeRateLbpPerUsd { get; set; } = 0m;

    /// <summary>
    /// When true, receipts show the LBP equivalent beneath USD totals.
    /// Requires ExchangeRateLbpPerUsd > 0.
    /// </summary>
    public bool ShowLbpOnReceipt { get; set; } = false;

    /// <summary>
    /// When true, a stamp duty line is added to USD-currency receipts
    /// as required by Lebanon's 2025 Budget Law.
    /// </summary>
    public bool StampDutyEnabled { get; set; } = false;

    /// <summary>
    /// The USD stamp duty amount per receipt (default $2.00 per 2025 Budget Law).
    /// Only applied when StampDutyEnabled = true and payment currency is USD.
    /// </summary>
    public decimal StampDutyAmountUsd { get; set; } = 2.00m;
}
