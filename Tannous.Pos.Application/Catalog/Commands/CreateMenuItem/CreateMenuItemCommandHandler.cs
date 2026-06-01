using MediatR;
using Tannous.Pos.Application.DTOs.Catalog;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Catalog.Commands.CreateMenuItem;

public class CreateMenuItemCommandHandler : IRequestHandler<CreateMenuItemCommand, MenuItemDto>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateMenuItemCommandHandler(
        IMenuItemRepository menuItemRepository,
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork)
    {
        _menuItemRepository = menuItemRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<MenuItemDto> Handle(CreateMenuItemCommand request, CancellationToken cancellationToken)
    {
        // Validate category exists
        var category = await _categoryRepository.GetByIdAsync(request.MenuItem.CategoryId);
        if (category == null)
            throw new ArgumentException($"Category with ID {request.MenuItem.CategoryId} not found");

        var menuItem = new MenuItem
        {
            Name = request.MenuItem.Name,
            Description = request.MenuItem.Description,
            Price = request.MenuItem.Price,
            IsActive = request.MenuItem.IsActive,
            ImageUrl = request.MenuItem.ImageUrl,
            DisplayOrder = request.MenuItem.DisplayOrder,
            HasAddOns = request.MenuItem.HasAddOns,
            HasIngredients = request.MenuItem.HasIngredients,
            CategoryId = request.MenuItem.CategoryId
        };

        await _menuItemRepository.AddAsync(menuItem);
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
