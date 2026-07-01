using MediatR;
using Tannous.Pos.Application.DTOs.Tables;

namespace Tannous.Pos.Application.Tables.Commands.CreateFloorPlan;

public class CreateFloorPlanCommand : IRequest<FloorPlanDto>
{
    public CreateFloorPlanDto FloorPlan { get; set; } = null!;
}
