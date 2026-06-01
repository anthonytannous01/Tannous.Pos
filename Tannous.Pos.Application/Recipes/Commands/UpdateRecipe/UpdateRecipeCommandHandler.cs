using MediatR;
using Tannous.Pos.Application.DTOs.Inventory;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Recipes.Commands.UpdateRecipe;

public class UpdateRecipeCommandHandler : IRequestHandler<UpdateRecipeCommand, RecipeDto>
{
    private readonly IRecipeRepository _recipeRepository;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IIngredientRepository _ingredientRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateRecipeCommandHandler(
        IRecipeRepository recipeRepository,
        IMenuItemRepository menuItemRepository,
        IIngredientRepository ingredientRepository,
        IUnitOfWork unitOfWork)
    {
        _recipeRepository = recipeRepository;
        _menuItemRepository = menuItemRepository;
        _ingredientRepository = ingredientRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<RecipeDto> Handle(UpdateRecipeCommand request, CancellationToken cancellationToken)
    {
        var recipe = await _recipeRepository.GetByIdWithDetailsAsync(request.Id);
        if (recipe == null)
            throw new ArgumentException($"Recipe with ID {request.Id} not found");

        // Validate menu item exists
        var menuItem = await _menuItemRepository.GetByIdAsync(request.Recipe.MenuItemId);
        if (menuItem == null)
            throw new ArgumentException($"Menu item with ID {request.Recipe.MenuItemId} not found");

        // Validate all ingredients exist
        foreach (var line in request.Recipe.Lines)
        {
            var ingredient = await _ingredientRepository.GetByIdAsync(line.IngredientId);
            if (ingredient == null)
                throw new ArgumentException($"Ingredient with ID {line.IngredientId} not found");
        }

        // Update recipe properties
        recipe.Name = request.Recipe.Name;
        recipe.Description = request.Recipe.Description;
        recipe.MenuItemId = request.Recipe.MenuItemId;
        recipe.UpdatedAt = DateTime.UtcNow;

        // Clear existing lines and add new ones
        recipe.RecipeLines.Clear();
        foreach (var lineDto in request.Recipe.Lines)
        {
            var recipeLine = new Tannous.Pos.Domain.Entities.RecipeLine
            {
                RecipeId = recipe.Id,
                IngredientId = lineDto.IngredientId,
                QuantityPerItem = lineDto.QuantityPerItem
            };
            recipe.RecipeLines.Add(recipeLine);
        }

        await _recipeRepository.UpdateAsync(recipe);
        await _unitOfWork.SaveChangesAsync();

        // Return updated recipe with lines
        return new RecipeDto
        {
            Id = recipe.Id,
            Name = recipe.Name,
            Description = recipe.Description,
            MenuItemId = recipe.MenuItemId,
            IsActive = recipe.IsActive,
            CreatedAt = recipe.CreatedAt,
            RecipeLines = recipe.RecipeLines.Select(rl => new RecipeLineDto
            {
                Id = rl.Id,
                IngredientId = rl.IngredientId,
                IngredientName = rl.Ingredient?.Name ?? string.Empty,
                QuantityPerItem = rl.QuantityPerItem,
                Unit = rl.Ingredient?.Unit ?? string.Empty
            }).ToList()
        };
    }
}
