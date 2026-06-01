using FluentValidation;
using Tannous.Pos.Application.Catalog.Commands.DeleteMenuItem;

namespace Tannous.Pos.Application.Catalog.Commands.DeleteMenuItem;

public class DeleteMenuItemCommandValidator : AbstractValidator<DeleteMenuItemCommand>
{
    public DeleteMenuItemCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Menu item ID is required");
    }
}
