namespace Tannous.Pos.Application.DTOs.Reports;

/// <summary>
/// Real-time sales summary for the owner dashboard.
/// Covers today (UTC) unless a custom date range is requested.
/// </summary>
public class SalesSummaryDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }

    // ── Core metrics ─────────────────────────────────────────────────────────
    public decimal NetSales { get; set; }
    public decimal TaxCollected { get; set; }
    public decimal StampDutyCollected { get; set; }
    public decimal GrossSales { get; set; }     // NetSales before tax/stamp
    public int OrdersCount { get; set; }
    public int VoidedOrdersCount { get; set; }
    public decimal VoidRate { get; set; }       // 0–100 percentage
    public decimal AvgTicket { get; set; }
    public decimal AvgItemsPerOrder { get; set; }

    // ── Order type split ─────────────────────────────────────────────────────
    public int DineInCount { get; set; }
    public int TakeawayCount { get; set; }
    public int DeliveryCount { get; set; }

    // ── Payment method split ─────────────────────────────────────────────────
    public List<PaymentMethodSummaryDto> PaymentMethods { get; set; } = new();

    // ── Top selling items ────────────────────────────────────────────────────
    public List<TopItemDto> TopItems { get; set; } = new();

    // ── Hourly breakdown (for sparkline/bar chart) ───────────────────────────
    public List<HourlySalesDto> HourlySales { get; set; } = new();
}

public class PaymentMethodSummaryDto
{
    public string Method { get; set; } = string.Empty;
    public string Currency { get; set; } = "USD";
    public decimal Amount { get; set; }
    public int Count { get; set; }
}

public class HourlySalesDto
{
    /// <summary>Hour of day (0–23) in UTC.</summary>
    public int Hour { get; set; }
    public decimal Sales { get; set; }
    public int Orders { get; set; }
}
