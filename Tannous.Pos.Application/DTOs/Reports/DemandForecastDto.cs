namespace Tannous.Pos.Application.DTOs.Reports;

/// <summary>
/// Rule-based demand forecast for a target date.
/// Same-day-of-week rolling average over the past 4 weeks — pure arithmetic, no ML.
/// </summary>
public class DemandForecastDto
{
    public DateTime TargetDate { get; set; }
    public string DayOfWeekName { get; set; } = string.Empty;
    /// <summary>Number of distinct same-day-of-week calendar weeks with data (0–4).</summary>
    public int WeeksOfDataUsed { get; set; }
    /// <summary>"Low" | "Medium" | "High" based on WeeksOfDataUsed.</summary>
    public string Confidence { get; set; } = "Low";
    public int EstimatedOrders { get; set; }
    public decimal EstimatedRevenue { get; set; }
    public List<TimeBlockForecastDto> TimeBlocks { get; set; } = new();
    public List<ItemForecastDto> TopItems { get; set; } = new();
    public List<IngredientDemandDto> IngredientDemands { get; set; } = new();
    /// <summary>Non-null when no historical data is available (WeeksOfDataUsed = 0).</summary>
    public string? InsufficientDataMessage { get; set; }
}

public class TimeBlockForecastDto
{
    /// <summary>Start hour of the 3-hour block (0, 3, 6, 9, 12, 15, 18, 21).</summary>
    public int StartHour { get; set; }
    public string Label { get; set; } = string.Empty;
    public int EstimatedOrders { get; set; }
    public decimal EstimatedSales { get; set; }
    /// <summary>True for the single highest-order block.</summary>
    public bool IsPeakBlock { get; set; }
}

public class ItemForecastDto
{
    public Guid MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    /// <summary>Average units sold on this day-of-week.</summary>
    public decimal AvgQty { get; set; }
    /// <summary>Rounded to nearest 0.5.</summary>
    public decimal EstimatedQty { get; set; }
}

public class IngredientDemandDto
{
    public Guid IngredientId { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>Reserved for future bilingual ingredient names (entity has none today).</summary>
    public string? NameAr { get; set; }
    public string Unit { get; set; } = string.Empty;
    /// <summary>Rounded to 2 decimal places.</summary>
    public decimal EstimatedQty { get; set; }
}
