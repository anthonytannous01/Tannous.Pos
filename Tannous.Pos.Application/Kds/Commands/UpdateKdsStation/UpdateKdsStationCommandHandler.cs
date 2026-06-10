using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Application.DTOs.Kds;
using Tannous.Pos.Application.Kds.Commands.CreateKdsStation;
using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Application.Kds.Commands.UpdateKdsStation;

public class UpdateKdsStationCommandHandler : IRequestHandler<UpdateKdsStationCommand, KdsStationDto>
{
    private readonly DbContext _dbContext;

    public UpdateKdsStationCommandHandler(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<KdsStationDto> Handle(UpdateKdsStationCommand request, CancellationToken cancellationToken)
    {
        var station = await _dbContext.Set<KdsStation>()
            .Include(s => s.MenuItems)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"KDS station {request.Id} not found.");

        var duplicate = await _dbContext.Set<KdsStation>()
            .AnyAsync(s =>
                s.IsActive &&
                s.Id != request.Id &&
                s.Name == request.Name &&
                s.BranchId == station.BranchId,
                cancellationToken);

        if (duplicate)
            throw new InvalidOperationException($"A station named '{request.Name}' already exists for this branch.");

        station.Name         = request.Name.Trim();
        station.NameAr       = request.NameAr?.Trim();
        station.Color        = request.Color;
        station.DisplayOrder = request.DisplayOrder;
        station.IsActive     = request.IsActive;
        station.UpdatedAt    = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var menuItemCount = station.MenuItems.Count(m => m.IsActive);
        return CreateKdsStationCommandHandler.MapToDto(station, menuItemCount);
    }
}
