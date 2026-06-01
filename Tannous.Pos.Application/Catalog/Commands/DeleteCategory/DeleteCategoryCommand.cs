using MediatR;

namespace Tannous.Pos.Application.Catalog.Commands.DeleteCategory;

public class DeleteCategoryCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public bool Force { get; set; } = false;
}
