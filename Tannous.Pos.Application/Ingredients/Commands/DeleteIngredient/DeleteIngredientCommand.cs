using MediatR;

namespace Tannous.Pos.Application.Ingredients.Commands.DeleteIngredient;

public class DeleteIngredientCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public bool Force { get; set; } = false;
}
