namespace Tannous.Pos.Application.DTOs.Reports;

/// <summary>
/// Kitchen performance analytics derived from completed KDS ticket timestamps.
/// Only OrderLines with KdsDoneAt set are included.
/// </summary>
public class KdsPerformanceDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int TotalTickets { get; set; }

    // ── Time-to-acknowledge (Order.CreatedAt → KdsAcknowledgedAt) ────────────
    public double AvgAcknowledgeSeconds { get; set; }
    public double P90AcknowledgeSeconds { get; set; }

    // ── Time-to-complete (KdsAcknowledgedAt → KdsDoneAt) ────────────────────
    public double AvgPrepSeconds { get; set; }
    public double P90PrepSeconds { get; set; }

    // ── Total ticket time (Order.CreatedAt → KdsDoneAt) ─────────────────────
    public double AvgTotalTicketSeconds { get; set; }
    public double P90TotalTicketSeconds { get; set; }

    // ── Throughput ────────────────────────────────────────────────────────────
    /// <summary>Tickets completed per hour, averaged across hours with completions.</summary>
    public double AvgThroughputPerHour { get; set; }
    /// <summary>The single busiest hour (0–23 UTC) by completed ticket count.</summary>
    public int? PeakThroughputHour { get; set; }
    public int? PeakThroughputCount { get; set; }

    public List<KdsHourlyDto> HourlyBreakdown { get; set; } = new();
    public List<KdsItemPerformanceDto> ItemBreakdown { get; set; } = new();
}

public class KdsHourlyDto
{
    public int Hour { get; set; }
    public int TicketsCompleted { get; set; }
    public double AvgTotalTicketSeconds { get; set; }
}

public class KdsItemPerformanceDto
{
    public Guid MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public int TicketCount { get; set; }
    public double AvgPrepSeconds { get; set; }
    public double P90PrepSeconds { get; set; }
}
