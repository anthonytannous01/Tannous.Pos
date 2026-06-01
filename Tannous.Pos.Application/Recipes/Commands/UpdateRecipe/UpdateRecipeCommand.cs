using MediatR;
using Tannous.Pos.Application.DTOs.Inventory;

namespace Tannous.Pos.Application.Recipes.Commands.UpdateRecipe;

public class UpdateRecipeCommand : IRequest<RecipeDto>
{
    public Guid Id { get; set; }
    public UpdateRecipeDto Recipe { get; set; } = new();
}
