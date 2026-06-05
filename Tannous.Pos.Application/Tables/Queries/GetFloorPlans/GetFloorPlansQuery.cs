using MediatR;
using Tannous.Pos.Application.DTOs.Tables;

namespace Tannous.Pos.Application.Tables.Queries.GetFloorPlans;

/// <summary>Returns all active floor plans with their tables and current status.</summary>
public class GetFloorPlansQuery : IRequest<List<FloorPlanDto>> { }
