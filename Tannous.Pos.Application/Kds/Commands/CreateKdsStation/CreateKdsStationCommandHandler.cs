using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Application.DTOs.Kds;
using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Application.Kds.Commands.CreateKdsStation;

public class CreateKdsStationCommandHandler : IRequestHandler<CreateKdsStationCommand, KdsStationDto>
{
    private readonly DbContext _dbContext;

    public CreateKdsStationCommandHandler(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<KdsStationDto> Handle(CreateKdsStationCommand request, CancellationToken cancellationToken)
    {
        var duplicate = await _dbContext.Set<KdsStation>()
            .AnyAsync(s =>
                s.IsActive &&
                s.Name == request.Name &&
                s.BranchId == request.BranchId,
                cancellationToken);

        if (duplicate)
            throw new InvalidOperationException($"A station named '{request.Name}' already exists for this branch.");

        var station = new KdsStation
        {
            Name         = request.Name.Trim(),
            NameAr       = request.NameAr?.Trim(),
            Color        = request.Color,
            DisplayOrder = request.DisplayOrder,
            BranchId     = request.BranchId,
            IsActive     = true
        };

        _dbContext.Set<KdsStation>().Add(station);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(station, menuItemCount: 0);
    }

    internal static KdsStationDto MapToDto(KdsStation station, int menuItemCount) => new()
    {
        Id            = station.Id,
        Name          = station.Name,
        NameAr        = station.NameAr,
        Color         = station.Color,
        DisplayOrder  = station.DisplayOrder,
        IsActive      = station.IsActive,
        BranchId      = station.BranchId,
        MenuItemCount = menuItemCount
    };
}
