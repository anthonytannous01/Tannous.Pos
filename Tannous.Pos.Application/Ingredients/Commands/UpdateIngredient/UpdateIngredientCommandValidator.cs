using FluentValidation;
using Tannous.Pos.Application.Ingredients.Commands.UpdateIngredient;

namespace Tannous.Pos.Application.Ingredients.Commands.UpdateIngredient;

public class UpdateIngredientCommandValidator : AbstractValidator<UpdateIngredientCommand>
{
    public UpdateIngredientCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Ingredient ID is required");

        RuleFor(x => x.Ingredient.Name)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Ingredient name is required and must not exceed 100 characters");

        RuleFor(x => x.Ingredient.Description)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Ingredient.Description))
            .WithMessage("Description must not exceed 500 characters");

        RuleFor(x => x.Ingredient.CostPerUnit)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Cost per unit must be zero or greater");

        RuleFor(x => x.Ingredient.Unit)
            .NotEmpty()
            .MaximumLength(20)
            .WithMessage("Unit is required and must not exceed 20 characters");
    }
}
