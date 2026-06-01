namespace Tannous.Pos.Application.DTOs.Settings;

public class SettingsDto
{
    public Guid Id { get; set; }

    /// <summary>
    /// JSON wire name <c>storeName</c> (camelCase). Android models may still use <c>businessName</c> for the same concept — keep wire stable; align clients rather than renaming this property.
    /// </summary>
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
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
