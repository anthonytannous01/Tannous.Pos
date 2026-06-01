using FluentValidation;
using Tannous.Pos.Application.Catalog.Commands.CreateCategory;

namespace Tannous.Pos.Application.Catalog.Commands.CreateCategory;

public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Category.Name)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Category name is required and must not exceed 100 characters");

        RuleFor(x => x.Category.Description)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Category.Description))
            .WithMessage("Description must not exceed 500 characters");

        RuleFor(x => x.Category.DisplayOrder)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Display order must be zero or greater");
    }
}
