using MediatR;
using Tannous.Pos.Application.DTOs.Inventory;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Recipes.Commands.CreateRecipe;

public class CreateRecipeCommandHandler : IRequestHandler<CreateRecipeCommand, RecipeDto>
{
    private readonly IRecipeRepository _recipeRepository;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IIngredientRepository _ingredientRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateRecipeCommandHandler(
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

    public async Task<RecipeDto> Handle(CreateRecipeCommand request, CancellationToken cancellationToken)
    {
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

        var recipe = new Recipe
        {
            Name = request.Recipe.Name,
            Description = request.Recipe.Description,
            MenuItemId = request.Recipe.MenuItemId,
            IsActive = true
        };

        // Create recipe lines
        foreach (var lineDto in request.Recipe.Lines)
        {
            var recipeLine = new RecipeLine
            {
                RecipeId = recipe.Id,
                IngredientId = lineDto.IngredientId,
                QuantityPerItem = lineDto.QuantityPerItem
            };
            recipe.RecipeLines.Add(recipeLine);
        }

        await _recipeRepository.AddAsync(recipe);
        await _unitOfWork.SaveChangesAsync();

        // Return recipe with lines
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
