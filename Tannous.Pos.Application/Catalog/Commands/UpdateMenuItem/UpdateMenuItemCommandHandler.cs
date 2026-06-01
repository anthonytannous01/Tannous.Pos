using MediatR;
using Tannous.Pos.Application.DTOs.Catalog;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Catalog.Commands.UpdateMenuItem;

public class UpdateMenuItemCommandHandler : IRequestHandler<UpdateMenuItemCommand, MenuItemDto>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateMenuItemCommandHandler(
        IMenuItemRepository menuItemRepository,
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork)
    {
        _menuItemRepository = menuItemRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<MenuItemDto> Handle(UpdateMenuItemCommand request, CancellationToken cancellationToken)
    {
        var menuItem = await _menuItemRepository.GetByIdAsync(request.Id);
        if (menuItem == null)
            throw new ArgumentException($"Menu item with ID {request.Id} not found");

        // Validate category exists
        var category = await _categoryRepository.GetByIdAsync(request.MenuItem.CategoryId);
        if (category == null)
            throw new ArgumentException($"Category with ID {request.MenuItem.CategoryId} not found");

        menuItem.Name = request.MenuItem.Name;
        menuItem.Description = request.MenuItem.Description;
        menuItem.Price = request.MenuItem.Price;
        menuItem.IsActive = request.MenuItem.IsActive;
        menuItem.ImageUrl = request.MenuItem.ImageUrl;
        menuItem.DisplayOrder = request.MenuItem.DisplayOrder;
        menuItem.HasAddOns = request.MenuItem.HasAddOns;
        menuItem.HasIngredients = request.MenuItem.HasIngredients;
        menuItem.CategoryId = request.MenuItem.CategoryId;
        menuItem.UpdatedAt = DateTime.UtcNow;

        await _menuItemRepository.UpdateAsync(menuItem);
        await _unitOfWork.SaveChangesAsync();

        return new MenuItemDto
        {
            Id = menuItem.Id,
            Name = menuItem.Name,
            Description = menuItem.Description,
            Price = menuItem.Price,
            IsActive = menuItem.IsActive,
            ImageUrl = menuItem.ImageUrl,
            DisplayOrder = menuItem.DisplayOrder,
            HasAddOns = menuItem.HasAddOns,
            HasIngredients = menuItem.HasIngredients,
            CategoryId = menuItem.CategoryId,
            CategoryName = category.Name,
            CreatedAt = menuItem.CreatedAt
        };
    }
}
