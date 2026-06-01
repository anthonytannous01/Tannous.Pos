using FluentValidation;
using Tannous.Pos.Application.Ingredients.Commands.DeleteIngredient;

namespace Tannous.Pos.Application.Ingredients.Commands.DeleteIngredient;

public class DeleteIngredientCommandValidator : AbstractValidator<DeleteIngredientCommand>
{
    public DeleteIngredientCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Ingredient ID is required");
    }
}
