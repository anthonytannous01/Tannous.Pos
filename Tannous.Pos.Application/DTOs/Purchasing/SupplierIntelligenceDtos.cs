namespace Tannous.Pos.Application.DTOs.Purchasing;

public class SupplierIntelligenceDto
{
    public DateTime GeneratedAt              { get; set; }
    public int      ForecastDays             { get; set; }
    public string   Confidence               { get; set; } = "Low";
    public List<LowStockAlertDto>            LowStockAlerts    { get; set; } = new();
    public List<SupplierOrderSuggestionDto>  OrderSuggestions  { get; set; } = new();
    public int      TotalIngredientsAnalysed { get; set; }
    public int      TotalSuggestedLines      { get; set; }
    public decimal  TotalEstimatedCost       { get; set; }
}

public class LowStockAlertDto
{
    public Guid    IngredientId { get; set; }
    public string  Name         { get; set; } = string.Empty;
    public string  Unit         { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal Deficit      { get; set; }
    public string? SupplierName { get; set; }
}

public class SupplierOrderSuggestionDto
{
    public Guid?   SupplierId         { get; set; }
    public string  SupplierName       { get; set; } = "Unassigned";
    public string? SupplierPhone      { get; set; }
    public string? SupplierEmail      { get; set; }
    public List<OrderLineSuggestionDto> Lines { get; set; } = new();
    public decimal TotalEstimatedCost { get; set; }
}

public class OrderLineSuggestionDto
{
    public Guid    IngredientId   { get; set; }
    public string  Name             { get; set; } = string.Empty;
    public string  Unit             { get; set; } = string.Empty;
    public decimal CurrentStock     { get; set; }
    public decimal MinimumStock     { get; set; }
    public decimal ProjectedUsage   { get; set; }
    public decimal SuggestedQty     { get; set; }
    public decimal UnitCost         { get; set; }
    public decimal EstimatedCost    { get; set; }
    public bool    IsLowStock       { get; set; }
}

public class CreateSuggestedOrdersDto
{
    public int         ForecastDays { get; set; } = 7;
    public Guid?       BranchId     { get; set; }
    public List<Guid>? SupplierIds { get; set; }
}

public class CreateSuggestedOrdersResult
{
    public int          OrdersCreated { get; set; }
    public List<Guid>   OrderIds      { get; set; } = new();
    public List<string> OrderNumbers  { get; set; } = new();
    public string?      SkippedReason { get; set; }
}
