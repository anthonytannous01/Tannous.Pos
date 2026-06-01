using MediatR;
using Tannous.Pos.Application.DTOs.Catalog;

namespace Tannous.Pos.Application.Catalog.Commands.CreateMenuItem;

public class CreateMenuItemCommand : IRequest<MenuItemDto>
{
    public CreateMenuItemDto MenuItem { get; set; } = new();
}
