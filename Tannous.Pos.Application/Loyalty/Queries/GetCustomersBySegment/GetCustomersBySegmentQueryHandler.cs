using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Application.DTOs.Common;
using Tannous.Pos.Application.DTOs.Loyalty;
using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Application.Loyalty.Queries.GetCustomersBySegment;

public class GetCustomersBySegmentQueryHandler
    : IRequestHandler<GetCustomersBySegmentQuery, PaginatedResponseDto<TopCustomerDto>>
{
    private readonly DbContext _dbContext;

    public GetCustomersBySegmentQueryHandler(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginatedResponseDto<TopCustomerDto>> Handle(
        GetCustomersBySegmentQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 50 : Math.Min(request.PageSize, 200);
        var utcNow = DateTime.UtcNow;

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

        var vipThreshold = CustomerSegmentEvaluator.ComputeVipThreshold(snapshots);

        var matching = snapshots
            .Where(s => CustomerSegmentEvaluator.DetermineSegment(s, vipThreshold, utcNow) == request.Segment)
            .OrderByDescending(s => s.LifetimePointsEarned)
            .ThenBy(s => s.Name)
            .ToList();

        var items = matching
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new TopCustomerDto
            {
                CustomerId           = s.CustomerId,
                Name                 = s.Name,
                Phone                = s.Phone,
                LifetimePointsEarned = s.LifetimePointsEarned,
                PointBalance         = s.PointBalance,
                TotalOrders          = s.TotalOrders,
                LastVisitDate        = s.LastVisitDate,
                Segment              = request.Segment
            })
            .ToList();

        return new PaginatedResponseDto<TopCustomerDto>
        {
            Items    = items,
            Total    = matching.Count,
            Page     = page,
            PageSize = pageSize
        };
    }
}
