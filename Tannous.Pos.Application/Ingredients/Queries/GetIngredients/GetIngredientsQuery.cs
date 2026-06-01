using MediatR;
using Tannous.Pos.Application.DTOs.Inventory;

namespace Tannous.Pos.Application.Ingredients.Queries.GetIngredients;

public class GetIngredientsQuery : IRequest<IEnumerable<IngredientDto>>
{
}
