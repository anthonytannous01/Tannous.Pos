using MediatR;

namespace Tannous.Pos.Application.Kds.Commands.DeleteKdsStation;

public class DeleteKdsStationCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}
