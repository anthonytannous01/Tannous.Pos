namespace Tannous.Pos.Application.DTOs.Settings;

public class UpdateSettingsDto
{
    public string StoreName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? TaxNumber { get; set; }
    public decimal TaxRate { get; set; }
    public string Currency { get; set; } = "USD";
    public bool TaxEnabled { get; set; }
    public string? ReceiptHeader { get; set; }
    public string? ReceiptFooter { get; set; }
    public bool RequireCustomerInfo { get; set; }
    public bool EnableInventoryTracking { get; set; }
    public bool EnableRecipeManagement { get; set; }

    // Lebanese market: dual-currency & stamp duty
    public decimal ExchangeRateLbpPerUsd { get; set; }
    public bool ShowLbpOnReceipt { get; set; }
    public bool StampDutyEnabled { get; set; }
    public decimal StampDutyAmountUsd { get; set; } = 2.00m;
}
