namespace Tannous.Pos.Application.DTOs.Reports;

public class EodReportDto
{
    public DateTime Date { get; set; }
    public decimal NetSales { get; set; }
    public int OrdersCount { get; set; }
    public decimal AvgTicket { get; set; }
    public List<TopItemDto> TopItems { get; set; } = new();
    public decimal CashDrops { get; set; }
    public decimal? Variance { get; set; }
}

public class TopItemDto
{
    public Guid ItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Qty { get; set; }
    public decimal Sales { get; set; }
}
