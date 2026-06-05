using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Application.DTOs.Tables;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Tables.Queries.GetFloorPlans;

public class GetFloorPlansQueryHandler : IRequestHandler<GetFloorPlansQuery, List<FloorPlanDto>>
{
    private readonly DbContext _dbContext;

    public GetFloorPlansQueryHandler(DbContext dbContext) => _dbContext = dbContext;

    public async Task<List<FloorPlanDto>> Handle(GetFloorPlansQuery request, CancellationToken cancellationToken)
    {
        var floorPlans = await _dbContext.Set<FloorPlan>()
            .Include(fp => fp.Tables)
            .Where(fp => fp.IsActive)
            .OrderBy(fp => fp.DisplayOrder)
            .ToListAsync(cancellationToken);

        // Load active orders per table in one query
        var occupiedTableIds = await _dbContext.Set<Order>()
            .Where(o => o.TableId != null &&
                       (o.Status == OrderStatus.Open || o.Status == OrderStatus.Pending))
            .Select(o => new { o.TableId, o.Id })
            .ToListAsync(cancellationToken);

        var activeOrderMap = occupiedTableIds
            .GroupBy(x => x.TableId!.Value)
            .ToDictionary(g => g.Key, g => g.First().Id);

        return floorPlans.Select(fp => new FloorPlanDto
        {
            Id           = fp.Id,
            Name         = fp.Name,
            Description  = fp.Description,
            DisplayOrder = fp.DisplayOrder,
            IsActive     = fp.IsActive,
            Tables       = fp.Tables
                .Where(t => t.IsActive)
                .OrderBy(t => t.DisplayOrder)
                .Select(t => new TableDto
                {
                    Id            = t.Id,
                    TableNumber   = t.TableNumber,
                    Label         = t.Label,
                    Capacity      = t.Capacity,
                    Status        = t.Status,
                    IsActive      = t.IsActive,
                    DisplayOrder  = t.DisplayOrder,
                    FloorPlanId   = fp.Id,
                    FloorPlanName = fp.Name,
                    ActiveOrderId = activeOrderMap.TryGetValue(t.Id, out var ordId) ? ordId : null
                })
                .ToList()
        }).ToList();
    }
}
