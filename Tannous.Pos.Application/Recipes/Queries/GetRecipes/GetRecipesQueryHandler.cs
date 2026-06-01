using MediatR;
using Tannous.Pos.Application.DTOs.Inventory;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Recipes.Queries.GetRecipes;

public class GetRecipesQueryHandler : IRequestHandler<GetRecipesQuery, IEnumerable<RecipeDto>>
{
    private readonly IRecipeRepository _recipeRepository;

    public GetRecipesQueryHandler(IRecipeRepository recipeRepository)
    {
        _recipeRepository = recipeRepository;
    }

    public async Task<IEnumerable<RecipeDto>> Handle(
        GetRecipesQuery query, CancellationToken cancellationToken)
    {
        // GetActiveRecipesAsync already includes MenuItem + RecipeLines + Ingredient — no new methods needed
        var recipes = await _recipeRepository.GetActiveRecipesAsync();
        return recipes.Select(MapToDto).ToList();
    }

    private static RecipeDto MapToDto(Recipe r) => new()
    {
        Id          = r.Id,
        Name        = r.Name,
        Description = r.Description,
        MenuItemId  = r.MenuItemId,
        IsActive    = r.IsActive,
        CreatedAt   = r.CreatedAt,
        RecipeLines = r.RecipeLines.Select(rl => new RecipeLineDto
        {
            Id              = rl.Id,
            IngredientId    = rl.IngredientId,
            IngredientName  = rl.Ingredient.Name,
            QuantityPerItem = rl.QuantityPerItem,
            Unit            = rl.Ingredient.Unit
        }).ToList()
    };
}
