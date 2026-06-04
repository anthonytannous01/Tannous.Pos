namespace Tannous.Pos.Application.DTOs.Reports;

/// <summary>
/// Menu engineering report — classifies each menu item by popularity vs margin.
///
/// Classic matrix (Kasavana &amp; Smith):
///   Stars       — high popularity, high margin  → promote, protect
///   Plowhorses  — high popularity, low margin   → reprice or reduce cost
///   Puzzles     — low popularity, high margin   → reposition, bundle, rename
///   Dogs        — low popularity, low margin    → remove or reprice
/// </summary>
public class MenuEngineeringReportDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int TotalOrders { get; set; }
    public List<MenuEngineeringItemDto> Items { get; set; } = new();
}

public class MenuEngineeringItemDto
{
    public Guid MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;

    // Volume
    public int UnitsSold { get; set; }
    public decimal PopularityIndex { get; set; }    // % of total units sold
    public bool IsHighPopularity { get; set; }

    // Financials
    public decimal Revenue { get; set; }
    public decimal CostOfGoods { get; set; }        // 0 if no recipe defined
    public decimal ContributionMargin { get; set; } // Revenue - COGS per unit
    public decimal ContributionMarginPct { get; set; }
    public bool IsHighMargin { get; set; }

    // Classification
    public MenuEngineeringCategory Category { get; set; }
}

public enum MenuEngineeringCategory
{
    Star      = 0,
    Plowhorse = 1,
    Puzzle    = 2,
    Dog       = 3
}
