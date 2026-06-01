using FluentValidation;
using Tannous.Pos.Application.Catalog.Commands.UpdateAddOn;

namespace Tannous.Pos.Application.Catalog.Commands.UpdateAddOn;

public class UpdateAddOnCommandValidator : AbstractValidator<UpdateAddOnCommand>
{
    public UpdateAddOnCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Add-on ID is required");

        RuleFor(x => x.AddOn.Name)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Add-on name is required and must not exceed 100 characters");

        RuleFor(x => x.AddOn.Description)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.AddOn.Description))
            .WithMessage("Description must not exceed 500 characters");

        RuleFor(x => x.AddOn.Price)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Price must be zero or greater");
    }
}
