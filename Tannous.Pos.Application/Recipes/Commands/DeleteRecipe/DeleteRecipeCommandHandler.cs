using MediatR;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Recipes.Commands.DeleteRecipe;

public class DeleteRecipeCommandHandler : IRequestHandler<DeleteRecipeCommand, bool>
{
    private readonly IRecipeRepository _recipeRepository;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteRecipeCommandHandler(
        IRecipeRepository recipeRepository,
        IMenuItemRepository menuItemRepository,
        IUnitOfWork unitOfWork)
    {
        _recipeRepository = recipeRepository;
        _menuItemRepository = menuItemRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteRecipeCommand request, CancellationToken cancellationToken)
    {
        var recipe = await _recipeRepository.GetByIdAsync(request.Id);
        if (recipe == null)
            throw new ArgumentException($"Recipe with ID {request.Id} not found");

        // Check if recipe is used by active menu items
        var menuItem = await _menuItemRepository.GetByIdAsync(recipe.MenuItemId);
        if (menuItem?.IsActive == true && !request.Force)
        {
            throw new InvalidOperationException(
                $"Cannot delete recipe '{recipe.Name}' because it is used by active menu item '{menuItem.Name}'. Use force=true to override.");
        }

        // If force=true, deactivate menu item first
        if (menuItem?.IsActive == true && request.Force)
        {
            menuItem.IsActive = false;
            menuItem.UpdatedAt = DateTime.UtcNow;
            await _menuItemRepository.UpdateAsync(menuItem);
        }

        await _recipeRepository.DeleteAsync(request.Id);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }
}
