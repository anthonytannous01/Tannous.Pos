namespace Tannous.Pos.Application.DTOs.Shifts;

public class CashDrawerEventDto
{
    public Guid Id { get; set; }
    public Guid ShiftId { get; set; }
    public string EventType { get; set; } = string.Empty; // SaleKick/NoSale/Open/Drop
    public decimal? Amount { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Note { get; set; }
}
