using FluentValidation;

namespace Tannous.Pos.Application.Inventory.Commands.RecordWastage;

public class RecordWastageCommandValidator : AbstractValidator<RecordWastageCommand>
{
    public RecordWastageCommandValidator()
    {
        RuleFor(x => x.OpId)
            .NotEmpty()
            .WithMessage("OpId is required");

        RuleFor(x => x.IngredientId)
            .NotEmpty()
            .WithMessage("IngredientId is required");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Wastage quantity must be greater than zero");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(500)
            .WithMessage("Reason is required and must not exceed 500 characters");
    }
}
