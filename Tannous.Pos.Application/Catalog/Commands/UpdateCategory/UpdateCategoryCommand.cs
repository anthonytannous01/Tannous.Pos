using MediatR;
using Tannous.Pos.Application.DTOs.Catalog;

namespace Tannous.Pos.Application.Catalog.Commands.UpdateCategory;

public class UpdateCategoryCommand : IRequest<CategoryDto>
{
    public Guid Id { get; set; }
    public UpdateCategoryDto Category { get; set; } = new();
}
