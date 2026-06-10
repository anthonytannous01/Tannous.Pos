using MediatR;
using Tannous.Pos.Application.DTOs.Kds;

namespace Tannous.Pos.Application.Kds.Queries.GetKdsStations;

/// <summary>
/// Returns active KDS stations for kitchen display filtering and management.
/// </summary>
public class GetKdsStationsQuery : IRequest<List<KdsStationDto>>
{
    public Guid? BranchId { get; set; }
}
