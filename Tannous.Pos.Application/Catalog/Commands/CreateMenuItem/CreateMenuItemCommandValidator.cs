using FluentValidation;
using Tannous.Pos.Application.Catalog.Commands.CreateMenuItem;

namespace Tannous.Pos.Application.Catalog.Commands.CreateMenuItem;

public class CreateMenuItemCommandValidator : AbstractValidator<CreateMenuItemCommand>
{
    public CreateMenuItemCommandValidator()
    {
        RuleFor(x => x.MenuItem.Name)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Menu item name is required and must not exceed 100 characters");

        RuleFor(x => x.MenuItem.Description)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.MenuItem.Description))
            .WithMessage("Description must not exceed 500 characters");

        RuleFor(x => x.MenuItem.Price)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Price must be zero or greater");

        RuleFor(x => x.MenuItem.CategoryId)
            .NotEmpty()
            .WithMessage("Category ID is required");

        RuleFor(x => x.MenuItem.DisplayOrder)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Display order must be zero or greater");
    }
}
