using MediatR;
using Tannous.Pos.Application.DTOs.Catalog;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Catalog.Queries.GetMenuItems;

public class GetMenuItemsQueryHandler : IRequestHandler<GetMenuItemsQuery, IEnumerable<MenuItemDto>>
{
    private readonly IMenuItemRepository _menuItemRepository;

    public GetMenuItemsQueryHandler(IMenuItemRepository menuItemRepository)
    {
        _menuItemRepository = menuItemRepository;
    }

    public async Task<IEnumerable<MenuItemDto>> Handle(
        GetMenuItemsQuery query, CancellationToken cancellationToken)
    {
        // All repository methods eager-load Category, so Category.Name is safe to access without a null check.
        IEnumerable<MenuItem> menuItems;
        if (query.CategoryId.HasValue)
        {
            menuItems = await _menuItemRepository.GetByCategoryAsync(query.CategoryId.Value);
        }
        else if (query.IncludeInactive)
        {
            menuItems = await _menuItemRepository.GetMenuItemsIncludingInactiveAsync();
        }
        else
        {
            menuItems = await _menuItemRepository.GetActiveMenuItemsAsync();
        }

        return menuItems.Select(MapToDto).ToList();
    }

    private static MenuItemDto MapToDto(MenuItem m) => new()
    {
        Id             = m.Id,
        Name           = m.Name,
        Description    = m.Description,
        NameAr         = m.NameAr,
        DescriptionAr  = m.DescriptionAr,
        Price          = m.Price,
        IsActive       = m.IsActive,
        ImageUrl       = m.ImageUrl,
        DisplayOrder   = m.DisplayOrder,
        HasAddOns      = m.HasAddOns,
        HasIngredients = m.HasIngredients,
        CategoryId     = m.CategoryId,
        CategoryName   = m.Category.Name,
        CreatedAt      = m.CreatedAt
    };
}
