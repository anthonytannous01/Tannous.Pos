using MediatR;
using Tannous.Pos.Application.DTOs.Inventory;

namespace Tannous.Pos.Application.Recipes.Commands.CreateRecipe;

public class CreateRecipeCommand : IRequest<RecipeDto>
{
    public CreateRecipeDto Recipe { get; set; } = new();
}
