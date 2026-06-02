using FluentValidation;

namespace Tannous.Pos.Application.Inventory.Commands.AdjustInventory;

public class AdjustInventoryCommandValidator : AbstractValidator<AdjustInventoryCommand>
{
    public AdjustInventoryCommandValidator()
    {
        RuleFor(x => x.OpId)
            .NotEmpty()
            .WithMessage("OpId is required");

        RuleFor(x => x.IngredientId)
            .NotEmpty()
            .WithMessage("IngredientId is required");

        RuleFor(x => x.Quantity)
            .NotEqual(0)
            .WithMessage("Adjustment quantity must not be zero");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(500)
            .WithMessage("Reason is required and must not exceed 500 characters");
    }
}
