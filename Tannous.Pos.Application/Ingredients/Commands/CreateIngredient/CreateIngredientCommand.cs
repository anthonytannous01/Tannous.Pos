using MediatR;
using Tannous.Pos.Application.DTOs.Inventory;

namespace Tannous.Pos.Application.Ingredients.Commands.CreateIngredient;

public class CreateIngredientCommand : IRequest<IngredientDto>
{
    public CreateIngredientDto Ingredient { get; set; } = new();
}
