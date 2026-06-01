using MediatR;
using Tannous.Pos.Application.DTOs.Inventory;

namespace Tannous.Pos.Application.Recipes.Queries.GetRecipes;

public class GetRecipesQuery : IRequest<IEnumerable<RecipeDto>>
{
}
