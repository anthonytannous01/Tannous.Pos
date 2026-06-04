namespace Tannous.Pos.Application.DTOs.Printing;

public class ReceiptTemplateDto
{
    public int LineWidth { get; set; } = 42;           // e.g., 42 for 80mm, 32 for 58mm
    public bool PrintLogo { get; set; } = false;
    public string? LogoUrl { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string Currency { get; set; } = "USD";
    public bool TaxEnabled { get; set; } = false;
    public string? Footer { get; set; }
    public string NumberFormat { get; set; } = "N0"; // currency formatting

    // Lebanese market
    public decimal ExchangeRateLbpPerUsd { get; set; } = 0m;
    public bool ShowLbpOnReceipt { get; set; } = false;
    public bool StampDutyEnabled { get; set; } = false;
    public decimal StampDutyAmountUsd { get; set; } = 2.00m;
}
