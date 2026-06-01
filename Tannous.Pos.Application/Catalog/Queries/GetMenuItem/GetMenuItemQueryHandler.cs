using MediatR;
using Tannous.Pos.Application.DTOs.Catalog;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Catalog.Queries.GetMenuItem;

public class GetMenuItemQueryHandler : IRequestHandler<GetMenuItemQuery, MenuItemDto?>
{
    private readonly IMenuItemRepository _menuItemRepository;

    public GetMenuItemQueryHandler(IMenuItemRepository menuItemRepository)
    {
        _menuItemRepository = menuItemRepository;
    }

    public async Task<MenuItemDto?> Handle(
        GetMenuItemQuery query, CancellationToken cancellationToken)
    {
        // GetByIdWithCategoryAsync includes Category; mapping uses null-safe access to match original behavior.
        var menuItem = await _menuItemRepository.GetByIdWithCategoryAsync(query.Id);
        if (menuItem == null) return null;
        return MapToDto(menuItem);
    }

    private static MenuItemDto MapToDto(MenuItem m) => new()
    {
        Id             = m.Id,
        Name           = m.Name,
        Description    = m.Description,
        Price          = m.Price,
        IsActive       = m.IsActive,
        ImageUrl       = m.ImageUrl,
        DisplayOrder   = m.DisplayOrder,
        HasAddOns      = m.HasAddOns,
        HasIngredients = m.HasIngredients,
        CategoryId     = m.CategoryId,
        CategoryName   = m.Category?.Name ?? string.Empty,
        CreatedAt      = m.CreatedAt
    };
}
