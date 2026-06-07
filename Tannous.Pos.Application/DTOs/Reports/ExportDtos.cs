namespace Tannous.Pos.Application.DTOs.Reports;

/// <summary>One row per paid order — for the accounting sales CSV export.</summary>
public class SalesExportRowDto
{
    public DateTime Date          { get; set; }
    public string   OrderNumber   { get; set; } = string.Empty;
    public string?  ReceiptNumber { get; set; }
    public string   OrderType     { get; set; } = string.Empty;
    public string?  CustomerName  { get; set; }
    public string?  BranchId      { get; set; }
    public decimal  SubTotal      { get; set; }
    public decimal  TaxAmount     { get; set; }
    public decimal  StampDuty     { get; set; }
    public decimal  Total         { get; set; }
    public decimal  Discount      { get; set; }
    public decimal  ChangeDue     { get; set; }
    /// <summary>Comma-separated payment methods if split payment.</summary>
    public string   PaymentMethods { get; set; } = string.Empty;
    public string   Currencies     { get; set; } = string.Empty;
}

/// <summary>One row per purchase order — for the accounting purchases CSV export.</summary>
public class PurchasesExportRowDto
{
    public DateTime Date            { get; set; }
    public string   OrderNumber     { get; set; } = string.Empty;
    public string   Supplier        { get; set; } = string.Empty;
    public string   Status          { get; set; } = string.Empty;
    public decimal  SubTotal        { get; set; }
    public decimal  TaxAmount       { get; set; }
    public decimal  Total           { get; set; }
    public string?  Notes           { get; set; }
    public int      LineCount       { get; set; }
}
