using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Application.Kds.Commands.DeleteKdsStation;

public class DeleteKdsStationCommandHandler : IRequestHandler<DeleteKdsStationCommand, bool>
{
    private readonly DbContext _dbContext;

    public DeleteKdsStationCommandHandler(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(DeleteKdsStationCommand request, CancellationToken cancellationToken)
    {
        var station = await _dbContext.Set<KdsStation>()
            .Include(s => s.MenuItems)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"KDS station {request.Id} not found.");

        foreach (var item in station.MenuItems.Where(m => m.KdsStationId == station.Id))
            item.KdsStationId = null;

        station.IsActive  = false;
        station.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
