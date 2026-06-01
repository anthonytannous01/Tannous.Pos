using MediatR;

namespace Tannous.Pos.Application.Recipes.Commands.DeleteRecipe;

public class DeleteRecipeCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public bool Force { get; set; } = false;
}
