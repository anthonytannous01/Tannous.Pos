using MediatR;

namespace Tannous.Pos.Application.Kds.Commands.AssignMenuItemsToStation;

public class AssignMenuItemsToStationCommand : IRequest<int>
{
    /// <summary>Target station, or null to unassign items.</summary>
    public Guid? StationId { get; set; }
    public List<Guid> MenuItemIds { get; set; } = new();
}
