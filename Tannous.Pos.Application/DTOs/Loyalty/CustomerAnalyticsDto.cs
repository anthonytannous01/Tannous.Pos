using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.DTOs.Loyalty;

public class CustomerAnalyticsDto
{
    public int TotalCustomers { get; set; }
    public int ActiveLast30Days { get; set; }
    public int AtRiskCount { get; set; }
    public int LapsedCount { get; set; }
    public int NewCount { get; set; }
    public int VipCount { get; set; }
    public decimal AverageOrderValue { get; set; }
    public decimal AveragePointBalance { get; set; }
    public List<TopCustomerDto> TopCustomers { get; set; } = new();
}

public class TopCustomerDto
{
    public Guid CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int LifetimePointsEarned { get; set; }
    public int PointBalance { get; set; }
    public int TotalOrders { get; set; }
    public DateTime? LastVisitDate { get; set; }
    public CustomerSegment Segment { get; set; }
}
