using MediatR;
using Tannous.Pos.Application.DTOs.Catalog;

namespace Tannous.Pos.Application.Catalog.Commands.UpdateMenuItem;

public class UpdateMenuItemCommand : IRequest<MenuItemDto>
{
    public Guid Id { get; set; }
    public UpdateMenuItemDto MenuItem { get; set; } = new();
}
