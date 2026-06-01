using MediatR;
using Tannous.Pos.Application.DTOs.Catalog;

namespace Tannous.Pos.Application.Catalog.Queries.GetMenuItem;

public class GetMenuItemQuery : IRequest<MenuItemDto?>
{
    public Guid Id { get; set; }
}
