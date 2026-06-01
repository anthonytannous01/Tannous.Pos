using MediatR;

namespace Tannous.Pos.Application.Catalog.Commands.DeleteAddOn;

public class DeleteAddOnCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public bool Force { get; set; } = false;
}
