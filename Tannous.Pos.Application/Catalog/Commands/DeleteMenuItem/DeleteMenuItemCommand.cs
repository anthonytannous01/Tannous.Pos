using MediatR;

namespace Tannous.Pos.Application.Catalog.Commands.DeleteMenuItem;

public class DeleteMenuItemCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public bool Force { get; set; } = false;
}
