using MediatR;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Catalog.Commands.DeleteCategory;

public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, bool>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCategoryCommandHandler(
        ICategoryRepository categoryRepository,
        IMenuItemRepository menuItemRepository,
        IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _menuItemRepository = menuItemRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(request.Id);
        if (category == null)
            throw new ArgumentException($"Category with ID {request.Id} not found");

        // Check if category has active menu items
        var menuItems = await _menuItemRepository.GetByCategoryAsync(request.Id);
        var activeMenuItems = menuItems.Where(m => m.IsActive).ToList();

        if (activeMenuItems.Any() && !request.Force)
        {
            throw new InvalidOperationException(
                $"Cannot delete category '{category.Name}' because it has {activeMenuItems.Count} active menu items. Use force=true to override.");
        }

        // If force=true, deactivate menu items first
        if (activeMenuItems.Any() && request.Force)
        {
            foreach (var menuItem in activeMenuItems)
            {
                menuItem.IsActive = false;
                menuItem.UpdatedAt = DateTime.UtcNow;
                await _menuItemRepository.UpdateAsync(menuItem);
            }
        }

        await _categoryRepository.DeleteAsync(request.Id);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }
}
