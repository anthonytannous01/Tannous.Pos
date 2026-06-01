using FluentValidation;
using Tannous.Pos.Application.Recipes.Commands.UpdateRecipe;
using Tannous.Pos.Application.DTOs.Inventory;

namespace Tannous.Pos.Application.Recipes.Commands.UpdateRecipe;

public class UpdateRecipeCommandValidator : AbstractValidator<UpdateRecipeCommand>
{
    public UpdateRecipeCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Recipe ID is required");

        RuleFor(x => x.Recipe.Name)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Recipe name is required and must not exceed 100 characters");

        RuleFor(x => x.Recipe.Description)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Recipe.Description))
            .WithMessage("Description must not exceed 500 characters");

        RuleFor(x => x.Recipe.MenuItemId)
            .NotEmpty()
            .WithMessage("Menu item ID is required");

        RuleFor(x => x.Recipe.Lines)
            .NotEmpty()
            .WithMessage("Recipe must have at least one line");

        RuleForEach(x => x.Recipe.Lines)
            .SetValidator(new UpdateRecipeLineDtoValidator());
    }
}

public class UpdateRecipeLineDtoValidator : AbstractValidator<UpdateRecipeLineDto>
{
    public UpdateRecipeLineDtoValidator()
    {
        RuleFor(x => x.IngredientId)
            .NotEmpty()
            .WithMessage("Ingredient ID is required");

        RuleFor(x => x.QuantityPerItem)
            .GreaterThan(0)
            .WithMessage("Quantity per item must be greater than zero");
    }
}
