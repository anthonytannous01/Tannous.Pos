using MediatR;
using Tannous.Pos.Application.DTOs.Catalog;

namespace Tannous.Pos.Application.Catalog.Queries.GetCategory;

public class GetCategoryQuery : IRequest<CategoryDto?>
{
    public Guid Id { get; set; }
}
