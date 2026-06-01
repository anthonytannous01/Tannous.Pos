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
}
