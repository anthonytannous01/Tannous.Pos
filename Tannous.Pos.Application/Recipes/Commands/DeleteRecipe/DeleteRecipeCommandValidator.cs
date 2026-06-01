using FluentValidation;
using Tannous.Pos.Application.Recipes.Commands.DeleteRecipe;

namespace Tannous.Pos.Application.Recipes.Commands.DeleteRecipe;

public class DeleteRecipeCommandValidator : AbstractValidator<DeleteRecipeCommand>
{
    public DeleteRecipeCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Recipe ID is required");
    }
}
