using MediatR;
using Tannous.Pos.Application.DTOs.Inventory;

namespace Tannous.Pos.Application.Ingredients.Commands.UpdateIngredient;

public class UpdateIngredientCommand : IRequest<IngredientDto>
{
    public Guid Id { get; set; }
    public UpdateIngredientDto Ingredient { get; set; } = new();
}
