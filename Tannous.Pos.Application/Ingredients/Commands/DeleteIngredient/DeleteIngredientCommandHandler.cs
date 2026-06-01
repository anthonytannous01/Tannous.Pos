using MediatR;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Ingredients.Commands.DeleteIngredient;

public class DeleteIngredientCommandHandler : IRequestHandler<DeleteIngredientCommand, bool>
{
    private readonly IIngredientRepository _ingredientRepository;
    private readonly IRecipeRepository _recipeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteIngredientCommandHandler(
        IIngredientRepository ingredientRepository,
        IRecipeRepository recipeRepository,
        IUnitOfWork unitOfWork)
    {
        _ingredientRepository = ingredientRepository;
        _recipeRepository = recipeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteIngredientCommand request, CancellationToken cancellationToken)
    {
        var ingredient = await _ingredientRepository.GetByIdAsync(request.Id);
        if (ingredient == null)
            throw new ArgumentException($"Ingredient with ID {request.Id} not found");

        // Check if ingredient is used in active recipes
        var recipes = await _recipeRepository.GetByIngredientAsync(request.Id);
        var activeRecipes = recipes.Where(r => r.IsActive).ToList();

        if (activeRecipes.Any() && !request.Force)
        {
            throw new InvalidOperationException(
                $"Cannot delete ingredient '{ingredient.Name}' because it is used in {activeRecipes.Count} active recipes. Use force=true to override.");
        }

        // If force=true, deactivate recipes first
        if (activeRecipes.Any() && request.Force)
        {
            foreach (var recipe in activeRecipes)
            {
                recipe.IsActive = false;
                recipe.UpdatedAt = DateTime.UtcNow;
                await _recipeRepository.UpdateAsync(recipe);
            }
        }

        await _ingredientRepository.DeleteAsync(request.Id);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }
}
