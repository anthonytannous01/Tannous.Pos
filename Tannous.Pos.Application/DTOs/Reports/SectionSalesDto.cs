namespace Tannous.Pos.Application.DTOs.Reports;

public class SectionSalesReportDto
{
    public DateTime From            { get; set; }
    public DateTime To              { get; set; }
    public int      TotalOrders     { get; set; }
    public decimal  TotalNetSales   { get; set; }
    public List<SectionSalesDto> Sections { get; set; } = new();
}

public class SectionSalesDto
{
    /// <summary>Floor plan name (e.g. "Indoor", "Terrace"). "No Section" for unassigned orders.</summary>
    public string   SectionName     { get; set; } = string.Empty;
    public bool     IsUnassigned    { get; set; }
    public int      OrderCount      { get; set; }
    public decimal  NetSales        { get; set; }
    public decimal  TaxCollected    { get; set; }
    public decimal  AvgTicket       { get; set; }
    /// <summary>This section's share of total net sales. 0–100 percentage, rounded to 1dp.</summary>
    public decimal  SharePercent    { get; set; }
    public List<SectionTopItemDto>  TopItems    { get; set; } = new();
    public List<SectionHourlyDto>   HourlySales { get; set; } = new();
}

public class SectionTopItemDto
{
    public Guid    MenuItemId { get; set; }
    public string  Name       { get; set; } = string.Empty;
    public string? NameAr     { get; set; }
    public int     Qty        { get; set; }
    public decimal Sales      { get; set; }
}

public class SectionHourlyDto
{
    public int     Hour   { get; set; }
    public int     Orders { get; set; }
    public decimal Sales  { get; set; }
}
