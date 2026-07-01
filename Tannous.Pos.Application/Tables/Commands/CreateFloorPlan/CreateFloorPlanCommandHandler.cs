using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Application.DTOs.Tables;
using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Application.Tables.Commands.CreateFloorPlan;

public class CreateFloorPlanCommandHandler : IRequestHandler<CreateFloorPlanCommand, FloorPlanDto>
{
    private readonly DbContext _dbContext;

    public CreateFloorPlanCommandHandler(DbContext dbContext) => _dbContext = dbContext;

    public async Task<FloorPlanDto> Handle(CreateFloorPlanCommand request, CancellationToken cancellationToken)
    {
        var dto = request.FloorPlan;

        var floorPlan = new FloorPlan
        {
            Name         = dto.Name.Trim(),
            Description  = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            DisplayOrder = dto.DisplayOrder
        };

        _dbContext.Set<FloorPlan>().Add(floorPlan);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new FloorPlanDto
        {
            Id           = floorPlan.Id,
            Name         = floorPlan.Name,
            Description  = floorPlan.Description,
            DisplayOrder = floorPlan.DisplayOrder,
            IsActive     = floorPlan.IsActive,
            Tables       = new()
        };
    }
}
