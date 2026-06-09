using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Application.DTOs.Loyalty;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Loyalty.Queries.GetCustomerAnalytics;

public class GetCustomerAnalyticsQueryHandler
    : IRequestHandler<GetCustomerAnalyticsQuery, CustomerAnalyticsDto>
{
    private readonly DbContext _dbContext;

    public GetCustomerAnalyticsQueryHandler(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CustomerAnalyticsDto> Handle(
        GetCustomerAnalyticsQuery request, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;

        // Project active loyalty accounts joined to their (non-deleted, active) customer.
        var snapshots = await _dbContext.Set<LoyaltyAccount>()
            .Where(la => la.IsActive
                && la.Customer.IsActive
                && !la.Customer.IsDeleted)
            .Select(la => new CustomerSegmentSnapshot
            {
                CustomerId           = la.CustomerId,
                Name                 = la.Customer.FirstName + " " + la.Customer.LastName,
                Phone                = la.Customer.Phone,
                LifetimePointsEarned = la.LifetimePointsEarned,
                PointBalance         = la.PointBalance,
                TotalOrders          = la.Customer.TotalOrders,
                LastVisitDate        = la.Customer.LastVisitDate
            })
            .ToListAsync(cancellationToken);

        var result = new CustomerAnalyticsDto
        {
            TotalCustomers = snapshots.Count
        };

        if (snapshots.Count == 0)
            return result;

        var vipThreshold = CustomerSegmentEvaluator.ComputeVipThreshold(snapshots);

        // Independent analytics counts (overlap allowed by design — see DTO comments).
        result.ActiveLast30Days = snapshots.Count(s =>
            s.LastVisitDate.HasValue &&
            (utcNow - s.LastVisitDate.Value).TotalDays <= CustomerSegmentEvaluator.ActiveWindowDays);

        result.AtRiskCount = snapshots.Count(s =>
            s.LastVisitDate.HasValue &&
            (utcNow - s.LastVisitDate.Value).TotalDays > CustomerSegmentEvaluator.ActiveWindowDays &&
            (utcNow - s.LastVisitDate.Value).TotalDays <= CustomerSegmentEvaluator.AtRiskWindowDays);

        result.LapsedCount = snapshots.Count(s =>
            (s.LastVisitDate.HasValue &&
             (utcNow - s.LastVisitDate.Value).TotalDays > CustomerSegmentEvaluator.AtRiskWindowDays) ||
            (s.LastVisitDate == null && s.TotalOrders > 0));

        result.NewCount = snapshots.Count(s => s.TotalOrders <= 2);

        result.VipCount = vipThreshold.HasValue
            ? snapshots.Count(s => s.LifetimePointsEarned >= vipThreshold.Value)
            : 0;

        result.AveragePointBalance = snapshots.Count > 0
            ? Math.Round((decimal)snapshots.Average(s => s.PointBalance), 2)
            : 0m;

        // Average order value across paid/finalized orders.
        var finalizedTotals = _dbContext.Set<Order>()
            .Where(o => o.Status == OrderStatus.Paid || o.Status == OrderStatus.Completed);

        result.AverageOrderValue = await finalizedTotals.AnyAsync(cancellationToken)
            ? Math.Round(await finalizedTotals.AverageAsync(o => o.TotalAmount, cancellationToken), 2)
            : 0m;

        result.TopCustomers = snapshots
            .OrderByDescending(s => s.LifetimePointsEarned)
            .Take(10)
            .Select(s => new TopCustomerDto
            {
                CustomerId           = s.CustomerId,
                Name                 = s.Name,
                Phone                = s.Phone,
                LifetimePointsEarned = s.LifetimePointsEarned,
                PointBalance         = s.PointBalance,
                TotalOrders          = s.TotalOrders,
                LastVisitDate        = s.LastVisitDate,
                Segment              = CustomerSegmentEvaluator.DetermineSegment(s, vipThreshold, utcNow)
            })
            .ToList();

        return result;
    }
}
