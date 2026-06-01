using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.DTOs.Shifts;

public class ShiftDto
{
    public Guid Id { get; set; }
    public string ShiftNumber { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public decimal? ClosingBalance { get; set; }
    public decimal? ExpectedCash { get; set; }
    public decimal? ActualCash { get; set; }
    public decimal? CashDifference { get; set; }
    public string? Notes { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }
}
