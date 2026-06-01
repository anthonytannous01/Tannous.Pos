using FluentValidation;
using Tannous.Pos.Application.Catalog.Commands.DeleteAddOn;

namespace Tannous.Pos.Application.Catalog.Commands.DeleteAddOn;

public class DeleteAddOnCommandValidator : AbstractValidator<DeleteAddOnCommand>
{
    public DeleteAddOnCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Add-on ID is required");
    }
}
