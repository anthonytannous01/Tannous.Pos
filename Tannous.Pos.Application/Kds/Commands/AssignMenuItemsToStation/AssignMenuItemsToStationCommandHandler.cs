using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Application.Kds.Commands.AssignMenuItemsToStation;

public class AssignMenuItemsToStationCommandHandler : IRequestHandler<AssignMenuItemsToStationCommand, int>
{
    private readonly DbContext _dbContext;

    public AssignMenuItemsToStationCommandHandler(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> Handle(AssignMenuItemsToStationCommand request, CancellationToken cancellationToken)
    {
        if (request.MenuItemIds.Count == 0)
            throw new ArgumentException("At least one menu item ID is required.");

        if (request.StationId.HasValue)
        {
            var stationExists = await _dbContext.Set<KdsStation>()
                .AnyAsync(s => s.Id == request.StationId.Value && s.IsActive, cancellationToken);

            if (!stationExists)
                throw new KeyNotFoundException($"KDS station {request.StationId} not found.");
        }

        var items = await _dbContext.Set<MenuItem>()
            .Where(m => request.MenuItemIds.Contains(m.Id))
            .ToListAsync(cancellationToken);

        foreach (var item in items)
            item.KdsStationId = request.StationId;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return items.Count;
    }
}
