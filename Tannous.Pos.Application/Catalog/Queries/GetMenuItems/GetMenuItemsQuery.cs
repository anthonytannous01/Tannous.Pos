using MediatR;
using Tannous.Pos.Application.DTOs.Catalog;

namespace Tannous.Pos.Application.Catalog.Queries.GetMenuItems;

public class GetMenuItemsQuery : IRequest<IEnumerable<MenuItemDto>>
{
    public Guid? CategoryId { get; set; }
}
