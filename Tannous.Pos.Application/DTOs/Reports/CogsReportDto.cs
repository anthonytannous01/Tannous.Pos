namespace Tannous.Pos.Application.DTOs.Reports;

public class CogsReportDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public decimal SalesTotal { get; set; }
    public decimal CogsTotal { get; set; }
    public decimal GrossMargin { get; set; }
    public List<CogsItemDto> IngredientUsage { get; set; } = new();
}

public class CogsItemDto
{
    public Guid IngredientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal QtyUsed { get; set; }
    public decimal Cost { get; set; }
}
