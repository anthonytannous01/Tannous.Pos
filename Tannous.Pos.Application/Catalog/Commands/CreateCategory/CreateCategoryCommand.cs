using MediatR;
using Tannous.Pos.Application.DTOs.Catalog;

namespace Tannous.Pos.Application.Catalog.Commands.CreateCategory;

public class CreateCategoryCommand : IRequest<CategoryDto>
{
    public CreateCategoryDto Category { get; set; } = new();
}
