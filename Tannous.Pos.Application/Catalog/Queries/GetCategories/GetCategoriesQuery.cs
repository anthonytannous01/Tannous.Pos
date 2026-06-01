using MediatR;
using Tannous.Pos.Application.DTOs.Catalog;

namespace Tannous.Pos.Application.Catalog.Queries.GetCategories;

public class GetCategoriesQuery : IRequest<IEnumerable<CategoryDto>>
{
}
