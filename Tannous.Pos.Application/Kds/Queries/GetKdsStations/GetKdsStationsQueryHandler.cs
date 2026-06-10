using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Application.DTOs.Kds;
using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Application.Kds.Queries.GetKdsStations;

public class GetKdsStationsQueryHandler : IRequestHandler<GetKdsStationsQuery, List<KdsStationDto>>
{
    private readonly DbContext _dbContext;

    public GetKdsStationsQueryHandler(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<KdsStationDto>> Handle(GetKdsStationsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<KdsStation>()
            .AsNoTracking()
            .Where(s => s.IsActive);

        if (request.BranchId.HasValue)
            query = query.Where(s => s.BranchId == request.BranchId);

        return await query
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.Name)
            .Select(s => new KdsStationDto
            {
                Id            = s.Id,
                Name          = s.Name,
                NameAr        = s.NameAr,
                Color         = s.Color,
                DisplayOrder  = s.DisplayOrder,
                IsActive      = s.IsActive,
                BranchId      = s.BranchId,
                MenuItemCount = s.MenuItems.Count(m => m.IsActive)
            })
            .ToListAsync(cancellationToken);
    }
}
